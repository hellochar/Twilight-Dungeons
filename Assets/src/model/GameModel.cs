using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Priority_Queue;
using UnityEngine.Events;
using System.Collections;

public class GameModel {
  public int seed;
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
  private List<Action> eventQueue = new List<Action>();

  public static GameModel main;

  public static void InitMain() {
    var seed = UnityEngine.Random.Range(0, 100000);
    main = new GameModel(seed);
    // main.generateGameModel();
    main.generateTinyFloorGameModel();
    var step = main.StepUntilPlayerChoice();
    // execute them all immediately
    do { } while (step.MoveNext());
  }

  public GameModel(int seed) {
    this.seed = seed;
    UnityEngine.Random.InitState(seed);
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

  public IEnumerator StepUntilPlayerChoice() {
    return turnManager.StepUntilPlayersChoice();
  }

  public void generateTinyFloorGameModel() {
    this.floors = new Floor[] {
      // FloorGenerator.EncounterTester(),
      FloorGenerator.generateRestFloor(0),
      FloorGenerator.generateTinyFloor(1, 9, 9),
      FloorGenerator.generateTinyFloor(2, 10, 10),
      FloorGenerator.generateTinyFloor(3, 11, 11),
      FloorGenerator.generateTinyFloor(4, 11, 11, 1),
      FloorGenerator.generateTinyFloor(5, 15, 15, 2),
      FloorGenerator.generateTinyFloor(6, 13, 13, 2),
      FloorGenerator.generateTinyFloor(7, 11, 11, 2),
      FloorGenerator.generateRewardFloor(8),
      FloorGenerator.generateTinyFloor(9, 13, 9, 2, 2),
      FloorGenerator.generateTinyFloor(10, 14, 7, 2, 2),
      FloorGenerator.generateTinyFloor(11, 20, 9, 3, 2),
      FloorGenerator.generateTinyFloor(12, 10, 10, 2, 2),
      FloorGenerator.generateTinyFloor(13, 12, 12, 3, 2),
      FloorGenerator.generateTinyFloor(14, 13, 13, 3, 2),
      FloorGenerator.generateTinyFloor(15, 15, 15, 4, 2),
      FloorGenerator.generateRewardFloor(16),
      FloorGenerator.generateRandomFloor(17, 15, 15, 4),
      FloorGenerator.generateRandomFloor(18, 20, 20, 4),
      FloorGenerator.generateRandomFloor(19, 30, 20, 5),
      FloorGenerator.generateRandomFloor(20, 20, 20, 6),
      FloorGenerator.generateRandomFloor(21, 24, 24, 8),
      FloorGenerator.generateRandomFloor(22, 30, 12, 10),
      FloorGenerator.generateRandomFloor(23, 30, 20, 15),
      FloorGenerator.generateRewardFloor(24)
    };

    this.player = new Player(floors[0].upstairs.landing);
    floors[0].Put(this.player);
  }

  internal void PutPlayerAt(Floor newFloor, bool isGoingUpstairs) {
    Floor oldFloor = player.floor;

    int newIndex = Array.FindIndex(floors, f => f == newFloor);
    this.activeFloorIndex = newIndex;
    Vector2Int newPlayerPosition;
    if (isGoingUpstairs) {
      newPlayerPosition = newFloor.downstairs.landing;
    } else {
      newPlayerPosition = newFloor.upstairs.landing;
    }
    oldFloor.Remove(player);
    oldFloor.RecordLastStepTime(this.time);
    player.pos = newPlayerPosition;
    newFloor.CatchUpStep(this.time);
    newFloor.Put(player);
  }

  internal void PutActorAt(Actor actor, Floor floor, Vector2Int pos) {
    var oldFloor = actor.floor;
    oldFloor.Remove(actor);
    actor.pos = pos;
    floor.Put(actor);
  }

  /// Get all actors that should be simulated, in no particular order. This includes: 
  /// SteppableEntity's on the current floor, and
  /// Plants on any floor
  internal IEnumerable<SteppableEntity> GetAllEntitiesInPlay() {
    var enumerable = Enumerable.Empty<SteppableEntity>();
    foreach (var f in floors) {
      if (f == currentFloor) {
        enumerable = enumerable.Concat(f.actors);
        enumerable = enumerable.Concat(f.grasses);
      } else {
        enumerable = enumerable.Concat(f.actors.Where((a) => a is Plant));
      }
    }
    return enumerable;
  }
}
