using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Priority_Queue;

public class GameModel {
  public Player player;
  public Floor[] floors;
  public int activeFloorIndex = 0;
  public int time;
  public TurnManager turnManager;
  public Floor currentFloor { get => floors[activeFloorIndex]; }

  public static GameModel main = new GameModel(); //new GameModel();
  static GameModel() {
    main.generateGameModel();
    var step = main.StepUntilPlayerChoice();
    // execute them all immediately
    do {} while (step.MoveNext());
  }

  public IEnumerator<object> StepUntilPlayerChoice() {
    if (turnManager == null) {
      turnManager = new TurnManager(this);
    }
    return turnManager.StepUntilPlayerChoice();
  }

  // TODO make all floor sets use this method
  public void ActivateFloor(Floor floor) {
    floor.CatchUpStep(this.time);
  }

  public void generateGameModel() {
    this.floors = new Floor[] {
      Floor.generateFloor0(),
      Floor.generateRandomFloor(),
      Floor.generateRandomFloor(),
      // Floor.generateRandomFloor(),
      // Floor.generateRandomFloor(),
      // Floor.generateRandomFloor(),
    };

    Tile floor0Upstairs = floors[0].upstairs;
    this.player = new Player(new Vector2Int(floor0Upstairs.pos.x + 1, floor0Upstairs.pos.y));
    player.floor = floors[0];
    // model.floors[0].actors.Add(model.player);
    floors[0].AddVisibility(player);
  }

  internal void PutPlayerAt(Floor nextFloor, bool isGoingUpstairs) {
    // Update active floor index
    // Put Player in new position after finding the connecting downstairs/upstairs
    // deactivate current floor
    int newIndex = Array.FindIndex(floors, f => f == nextFloor);
    this.activeFloorIndex = newIndex;
    Vector2Int newPlayerPosition;
    if (isGoingUpstairs) {
      newPlayerPosition = this.currentFloor.downstairs.pos + new Vector2Int(-1, 0);
    } else {
      newPlayerPosition = this.currentFloor.upstairs.pos + new Vector2Int(1, 0);
    }
    player.pos = newPlayerPosition;
    player.floor = nextFloor;
  }
}

public class TurnManager {
  private SimplePriorityQueue<Actor, float> queue = new SimplePriorityQueue<Actor, float>();
  private GameModel model { get; }
  public TurnManager(GameModel model) {
    this.model = model;
    AddFloor(model.currentFloor);
    AddActor(model.player);
  }

  public override string ToString() {
    return String.Join(", ", queue.Select(a => {
      float shiftedPriority = queue.GetPriority(a);
      return $"{shiftedPriority} - {a}";
    }));
  }

  public void AddActor(Actor actor) {
    if (queue.Contains(actor)) {
      queue.Remove(actor);
    }
    float shiftedSchedule = actor.timeNextAction + actor.queueOrderOffset;
    queue.Enqueue(actor, shiftedSchedule);
    Debug.Log(actor + " scheduled at " + shiftedSchedule + ". Queue is now " + this);
  }

  public void RemoveActor(Actor actor) {
    queue.Remove(actor);
  }

  private void AddFloor(Floor floor) {
    foreach (Actor a in floor.actors) {
      AddActor(a);
    }
  }

  public void RemoveFloor(Floor floor) {
    foreach (Actor a in floor.actors) {
      RemoveActor(a);
    }
  }

  internal IEnumerator<object> StepUntilPlayerChoice() {
    // int nextYieldTime = time + 1;
    do {
      if (queue.First == model.player && model.player.action == null) {
        yield break;
      }
      Actor actor = queue.Dequeue();

      if (model.time > actor.timeNextAction) {
        throw new Exception("time is " + model.time + " but " + actor + " had a turn at " + actor.timeNextAction);
      }

      if (model.time != actor.timeNextAction) {
        Debug.Log("Progressing time from " + model.time + " to " + actor.timeNextAction);
        // move game time up to now
        model.time = actor.timeNextAction;
      }

      // move forward
      actor.Step();
      Debug.Log(actor + " acted!");

      // Put actor back in queue
      AddActor(actor);
      // Debug.Log($"Stepped {actor}: " + String.Join(", ", queue.Select(a => $"[{a} ({a.timeNextAction}) - {queue.GetPriority(a)}]")));
      // if (time > nextYieldTime) {
        // nextYieldTime = time + 1;
      yield return new WaitForSeconds(0.5f);
    } while (true);
  }
}