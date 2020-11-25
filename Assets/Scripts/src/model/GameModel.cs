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
  public float time;
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
    newFloor.AddActor(player);

    player.floor.CatchUpStep(this.time);
  }

  /// Get all actors that should be simulated, in no order. This includes: 
  /// Actors on the current floor, and
  /// Plants on any floor
  internal IEnumerable<Actor> GetAllActorsInPlay() {
    IEnumerable<Actor> enumerable = Enumerable.Empty<Actor>();
    foreach (var f in floors) {
      if (f == currentFloor) {
        enumerable = enumerable.Concat(f.Actors());
      } else {
        enumerable = enumerable.Concat(f.Actors().Where((a) => a is Plant));
      }
    }
    return enumerable;
  }
}

public class TurnManager {
  // private SimplePriorityQueue<Actor, float> queue = new SimplePriorityQueue<Actor, float>();
  private GameModel model { get; }
  public event Action OnPlayersChoice;
  public TurnManager(GameModel model) {
    this.model = model;
  }

  /// The actor whose turn it is
  private Actor FindActiveActor() {
    var allActorsInPlay = model.GetAllActorsInPlay();
    return allActorsInPlay.Aggregate((a1, a2) => {
      if (a1.timeNextAction == a2.timeNextAction) {
        return a1.turnPriority < a2.turnPriority ? a1 : a2;
      }
      return a1.timeNextAction < a2.timeNextAction ? a1 : a2;
    });
  }

  internal IEnumerator<object> StepUntilPlayersChoice(Action onEnd) {
    // skip one frame so whatever's currently running can finish (in response to player.action = xxx)
    yield return null;

    model.DrainEventQueue();
    bool isFirstIteration = true;
    do {
      Actor actor = FindActiveActor();
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

      actor.Step();

      if (!isFirstIteration) {
        // stagger actors just a bit for juice
        yield return new WaitForSeconds(.016f);
      }

      model.DrainEventQueue();
      isFirstIteration = false;
    } while (true);

    OnPlayersChoice?.Invoke();
    onEnd();
  }
}