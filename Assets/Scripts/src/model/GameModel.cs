﻿using System;
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

  /// Events to process in response to state changes
  public List<Action> eventQueue = new List<Action>();

  public static GameModel main = new GameModel(); //new GameModel();
  static GameModel() {
    main.generateGameModel();
    var step = main.StepUntilPlayerChoice(() => {});
    // execute them all immediately
    do {} while (step.MoveNext());
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
    if (turnManager == null) {
      turnManager = new TurnManager(this);
    }
    return turnManager.StepUntilPlayerChoice(onEnd);
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
    /// Stop stepping old floor
    this.turnManager.RemoveFloor(oldFloor);
    oldFloor.RemoveVisibility(player);

    int newIndex = Array.FindIndex(floors, f => f == newFloor);
    this.activeFloorIndex = newIndex;
    Vector2Int newPlayerPosition;
    if (isGoingUpstairs) {
      newPlayerPosition = this.currentFloor.downstairs.pos + new Vector2Int(-1, 0);
    } else {
      newPlayerPosition = this.currentFloor.upstairs.pos + new Vector2Int(1, 0);
    }
    player.pos = newPlayerPosition;
    // Add player. Important to do this before CatchUpStep because actors may move over player position
    newFloor.AddActor(player);
    newFloor.AddVisibility(player);

    player.floor.CatchUpStep(this.time);
    this.turnManager.AddFloor(newFloor);
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
    // Debug.Log(actor + " scheduled at " + shiftedSchedule + ". Queue is now " + this);
  }

  public void RemoveActor(Actor actor) {
    queue.Remove(actor);
  }

  public void AddFloor(Floor floor) {
    foreach (Actor a in floor.Actors()) {
      AddActor(a);
    }
  }

  public void RemoveFloor(Floor floor) {
    foreach (Actor a in floor.Actors()) {
      RemoveActor(a);
    }
  }

  /// TODO - fix bug - multiple of these coroutines may be running at once!
  internal IEnumerator<object> StepUntilPlayerChoice(Action onEnd) {
    model.DrainEventQueue();
    // int nextYieldTime = time + 1;
    bool isFirstIteration = true;
    do {
      if (queue.First == model.player && model.player.action == null) {
        break;
      }
      Actor actor = queue.Dequeue();

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

      // move forward
      actor.Step();
      // Debug.Log(actor + " acted!");

      // Put actor back in queue
      AddActor(actor);
      model.DrainEventQueue();
      isFirstIteration = false;
    } while (true);

    onEnd();
  }
}