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
    #if DEBUG
    // seed = 53922;
    #endif
    main = new GameModel(seed);
    main.generate();
    var step = main.StepUntilPlayerChoice();
    // execute them all immediately
    do { } while (step.MoveNext());
  }

  public GameModel(int seed) {
    this.seed = seed;
    UnityEngine.Random.InitState(seed);
  }

  private void generate() {
    var floorGen = new FloorGenerator(new Encounters());
    floors = new Floor[] {
      floorGen.generateRestFloor(0),
      floorGen.generateSingleRoomFloor(1, 9, 9),
      floorGen.generateSingleRoomFloor(2, 10, 10),
      floorGen.generateSingleRoomFloor(3, 11, 11),
      floorGen.generateSingleRoomFloor(4, 11, 11, 1, 1, true),
      floorGen.generateSingleRoomFloor(5, 15, 15, 2),
      floorGen.generateSingleRoomFloor(6, 13, 13, 2),
      floorGen.generateSingleRoomFloor(7, 11, 11, 2),
      floorGen.generateRewardFloor(8),
      floorGen.generateSingleRoomFloor(9, 13, 9, 2, 2),
      floorGen.generateSingleRoomFloor(10, 14, 7, 2, 2),
      floorGen.generateSingleRoomFloor(11, 20, 9, 3, 2),
      floorGen.generateSingleRoomFloor(12, 10, 10, 2, 2, true),
      floorGen.generateSingleRoomFloor(13, 12, 12, 3, 2),
      floorGen.generateSingleRoomFloor(14, 13, 13, 3, 2),
      floorGen.generateSingleRoomFloor(15, 15, 15, 4, 2),
      floorGen.generateRewardFloor(16),
      floorGen.generateMultiRoomFloor(17, 15, 15, 4),
      floorGen.generateMultiRoomFloor(18, 20, 20, 4),
      floorGen.generateMultiRoomFloor(19, 30, 20, 5),
      floorGen.generateMultiRoomFloor(20, 20, 20, 6, true),
      floorGen.generateMultiRoomFloor(21, 24, 24, 8),
      floorGen.generateMultiRoomFloor(22, 30, 12, 10),
      floorGen.generateMultiRoomFloor(23, 30, 20, 15),
      floorGen.generateRewardFloor(24)
    };

    player = new Player(floors[0].upstairs.landing);
    floors[0].Put(player);
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
