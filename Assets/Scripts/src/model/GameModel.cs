using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Priority_Queue;
using UnityEngine.Events;

public class GameModel {
  public Player player;
  public Floor[] floors;
  public int activeFloorIndex = 0;
  public int time;
  private TurnManager _turnManager;
  public TurnManager turnManager {
    get {
      if (_turnManager == null) {
        _turnManager = new TurnManager(this);
      }
      return _turnManager;
    }
  }
  public Floor currentFloor { get => floors[activeFloorIndex]; }

  /// Events to process in response to state changes
  public List<Action> eventQueue = new List<Action>();

  public static GameModel main = new GameModel(); //new GameModel();
  static GameModel() {
    main.generateGameModel();
    var step = main.StepUntilPlayerChoice(() => { });
    // execute them all immediately
    do { } while (step.MoveNext());
  }

  public void EnqueueEvent(Action cb) {
    eventQueue.Add(cb);
  }

  internal void DrainEventQueue() {
    // take care - events could add more events, which then add more events
    // guard against infinite events
    int maxEventGenerations = 32;
    for (int generation = 0; generation < maxEventGenerations; generation++) {
      // clone event queue
      List<Action> queue = new List<Action>(eventQueue);

      // free up global event queue to capture new events
      eventQueue.Clear();

      // invoke actions in this generation
      queue.ForEach(a => a());

      // if no more triggers, we're done
      if (eventQueue.Count == 0) {
        return;
      }
    }
    throw new System.Exception("Reached max event queue generations!");
  }

  public IEnumerator<object> StepUntilPlayerChoice(Action onEnd) {
    return turnManager.StepUntilPlayersChoice(onEnd);
  }

  public void generateGameModel() {
    this.floors = new Floor[] {
      FloorGenerator.generateFloor0(),
      FloorGenerator.generateRandomFloor(),
      FloorGenerator.generateRandomFloor(),
      // Floor.generateRandomFloor(),
      // Floor.generateRandomFloor(),
      // Floor.generateRandomFloor(),
    };

    Tile floor0Upstairs = floors[0].upstairs;
    this.player = new Player(new Vector2Int(floor0Upstairs.pos.x + 1, floor0Upstairs.pos.y));
    floors[0].AddActor(this.player);
    floors[0].AddVisibility(player);
  }

  internal void PutPlayerAt(Floor newFloor, bool isGoingUpstairs) {
    Floor oldFloor = player.floor;

    int newIndex = Array.FindIndex(floors, f => f == newFloor);
    this.activeFloorIndex = newIndex;
    Vector2Int newPlayerPosition;
    if (isGoingUpstairs) {
      newPlayerPosition = newFloor.downstairs.pos + new Vector2Int(-1, 0);
    } else {
      newPlayerPosition = newFloor.upstairs.pos + new Vector2Int(1, 0);
    }
    oldFloor.RemoveActor(player);
    player.pos = newPlayerPosition;
    // Add player. Important to do this before CatchUpStep because actors may move over player position
    newFloor.AddActor(player);

    player.floor.CatchUpStep(this.time);
  }
}

public class TurnManager {
  // private SimplePriorityQueue<Actor, float> queue = new SimplePriorityQueue<Actor, float>();
  private GameModel model { get; }
  public event Action OnPlayersChoice;
  public TurnManager(GameModel model) {
    this.model = model;
    // AddFloor(model.currentFloor);
    // AddActor(model.player);
  }

  // public override string ToString() {
  //   return String.Join(", ", queue.Select(a => {
  //     float shiftedPriority = queue.GetPriority(a);
  //     return $"{shiftedPriority} - {a}";
  //   }));
  // }

  // public void AddActor(Actor actor) {
  //   if (queue.Contains(actor)) {
  //     queue.Remove(actor);
  //   }
  //   float shiftedSchedule = actor.timeNextAction + actor.queueOrderOffset;
  //   queue.Enqueue(actor, shiftedSchedule);
  // }

  // public void RemoveActor(Actor actor) {
  //   queue.Remove(actor);
  // }

  // public void AddFloor(Floor floor) {
  //   foreach (Actor a in floor.Actors()) {
  //     AddActor(a);
  //   }
  // }

  // public void RemoveFloor(Floor floor) {
  //   foreach (Actor a in floor.Actors()) {
  //     RemoveActor(a);
  //   }
  // }

  /// The actor whose turn it is
  private Actor FindActiveActor() {
    return model.currentFloor.Actors().Aggregate((a1, a2) => a1.timeNextAction < a2.timeNextAction ? a1 : a2);
  }

  internal IEnumerator<object> StepUntilPlayersChoice(Action onEnd) {
    // skip one frame so whatever's currently running can finish (in response to player.action = xxx)
    yield return null;

    model.DrainEventQueue();
    bool isFirstIteration = true;
    do {
      Actor actor = FindActiveActor(); //queue.Dequeue();
      if (actor == model.player && model.player.action == null) {
        break;
      }

      if (model.time > actor.timeNextAction) {
        throw new Exception("time is " + model.time + " but " + actor + " had a turn at " + actor.timeNextAction);
      }

      if (model.time != actor.timeNextAction) {
        // Debug.Log("Progressing time from " + model.time + " to " + actor.timeNextAction);
        // The first iteration will usually be right after the user's set an action.
        // Do *not* pause in that situation to allow the game to respond instantly.
        if (!isFirstIteration) {
          yield return new WaitForSeconds((actor.timeNextAction - model.time) * 0.2f);
        }
        // move game time up to now
        model.time = actor.timeNextAction;
      }

      // if (actor == model.player) {
      //   await model.player.WaitUntilActionIsDecided();
      // }

      // move forward
      actor.Step();

      // drain events
      model.DrainEventQueue();
      isFirstIteration = false;
    } while (true);

    OnPlayersChoice?.Invoke();
    onEnd();
  }
}