using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Priority_Queue;
using UnityEngine.Events;
using System.Collections;
using System.Runtime.Serialization;

[Serializable]
public class GameModel {
  public int seed;
  public Player player;

  /// We will regenerate this by saving the Seed for each unexplored floor. A few conditions:
  /// * You never visit an "older" floor once it's "completed"
  /// * A floor can be completely generated from just a Seed number, plus the random generation algorithm (TODO-SERIALIZE we'll need to store encounter weights)
  /// The primary reason for this is that both AIActor#ai and Actor#ActorTask are type IEnumerable, which is impossible to serialize
  [NonSerialized]
  public Floor[] floors;
  /// floor0, on the other hand, *will* be saved.
  public Floor floor0;

  public int activeFloorIndex = 0;
  public float time;
  [NonSerialized]
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

  [field:NonSerialized]
  public event Action<Floor, Floor> OnPlayerChangeFloor;

  /// Events to process in response to state changes
  [NonSerialized]
  private List<Action> eventQueue = new List<Action>();

  public static GameModel main;
  public static bool shouldLoad = false;

  public static void InitOrLoadMain() {
    GameModel model;
    if (shouldLoad && Serializer.LoadFromFile(out model)) {
      shouldLoad = false;
      main = model;
      main.RehookUpAfterSerialization();
    } else {
      var seed = UnityEngine.Random.Range(0, 100000);

      #if UNITY_EDITOR
      // seed = 33415;
      // Analyze();
      #endif

      main = new GameModel(seed);
      main.generate();
      var step = main.StepUntilPlayerChoice();
      // execute them all immediately
      do { } while (step.MoveNext());
    }
  }

  public GameModel(int seed) {
    this.seed = seed;
    UnityEngine.Random.InitState(seed);
  }

  private void generate() {
    floors = FloorGenerator.generateAll();
    floor0 = floors[0];
    player = new Player(new Vector2Int(3, floors[0].height/2));
    floors[0].Put(player);
  }

  // [OnDeserialized]
  public void RehookUpAfterSerialization() {
    eventQueue = new List<Action>();
    /// TODO-SERIALIZATION generation is different because
    /// seed is different
    floors = FloorGenerator.generateAll();
    floors[0] = floor0;
  }

  private static void Analyze() {
    var dict = new Dictionary<Type, int[]>();
    for (int i = 0; i < 10; i++) {
      main = new GameModel(UnityEngine.Random.Range(0, 999999));
      main.generate();
      // Analyze(model, i, floor => floor.actors.Where((a) => a.faction == Faction.Enemy));
      Analyze(main, i, floor => floor.grasses);
    }
    var l = dict.ToList().OrderByDescending((pair) => pair.Value[0]);
    var s = String.Join("\n", l.Select((pair) => $"{pair.Key}, {String.Join(", ", pair.Value)}"));
    Debug.Log(s);

    void Analyze<T>(GameModel model, int index, Func<Floor, IEnumerable<T>> selector) {
      foreach (var floor in model.floors) {
        foreach (var actor in selector(floor)) {
          var t = actor.GetType();
          if (!dict.ContainsKey(t)) {
            dict.Add(t, new int[10]);
          }
          dict[t][index]++;
        }
      }
    }
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
      newPlayerPosition = newFloor.upstairs?.landing ?? new Vector2Int(newFloor.width / 2, newFloor.height/ 2);
    }
    oldFloor.Remove(player);
    oldFloor.RecordLastStepTime(this.time);
    player.pos = newPlayerPosition;
    newFloor.CatchUpStep(this.time);
    newFloor.Put(player);
    OnPlayerChangeFloor?.Invoke(oldFloor, newFloor);
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
  internal IEnumerable<ISteppable> GetAllEntitiesInPlay() {
    var enumerable = Enumerable.Empty<ISteppable>();
    foreach (var f in floors) {
      if (f == currentFloor) {
        enumerable = enumerable.Concat(f.steppableEntities);
      } else {
        enumerable = enumerable.Concat(f.bodies.Where((a) => a is Plant).Cast<Plant>());
      }
    }
    return enumerable;
  }
}
