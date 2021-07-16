using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Runtime.Serialization;

[Serializable]
public class GameModel {
  public int seed;
  public Player player;
  public Floor home;
  public Floor cave;
  public int depth = 0;
  public float time = 0;
  public List<int> floorSeeds;
  public FloorGenerator generator;

  private TurnManager _turnManager;
  public TurnManager turnManager {
    get {
      if (_turnManager == null) {
        _turnManager = new TurnManager(this);
      }
      return _turnManager;
    }
  }
  public Floor currentFloor => depth == 0 ? home : cave;

  [field:NonSerialized] /// Controller only
  public event Action<Floor, Floor> OnPlayerChangeFloor;

  [NonSerialized] /// Controller only
  public Action<Boss> OnBossSeen = delegate {};

  /// Events to process in response to state changes
  [NonSerialized] /// This should be empty on save
  private List<Action> eventQueue = new List<Action>();

  public TimedEventManager timedEvents = new TimedEventManager();
  public static GameModel main;

  /// Also sets GameModel.main.
  public static void GenerateNewGameAndSetMain() {
    main = new GameModel();
    main.generate();
    var step = main.StepUntilPlayerChoice();
    // execute them all immediately
    do { } while (step.MoveNext());
  }

  public static void GenerateTutorialAndSetMain() {
    main = new GameModel();
    main.generateTutorial();
  }

  public GameModel() {
    this.seed = new System.Random().Next();
    #if UNITY_EDITOR
    // this.seed = 0x6b1b3282;
    #endif
  }

  #if UNITY_EDITOR
  [OnSerializing]
  void HandleSerializing() {
    if (eventQueue.Any()) {
      Debug.LogError("Serializing during not null event queue! " + eventQueue.Count);
      DrainEventQueue();
    }
  }
  #endif

  [OnDeserialized]
  void HandleDeserialized() {
    eventQueue = new List<Action>();
  }

  private void generate() {
    Debug.Log("generating from seed " + seed.ToString("X"));
    MyRandom.SetSeed(seed);
    floorSeeds = new List<int>();
    /// generate floor seeds first
    for (int i = 0; i < 33; i++) {
      floorSeeds.Add(MyRandom.Next());
    }
    generator = new FloorGenerator(floorSeeds);
    home = generator.generateCaveFloor(0);
    cave = generator.generateCaveFloor(1);
    player = new Player(new Vector2Int(2, home.height/2));
    home.Put(player);
  }

  private void generateTutorial() {
    player = new Player(new Vector2Int(2, 4));
    // player = new Player(new Vector2Int(50, 4));
    MyRandom.SetSeed(seed);
    floorSeeds = new List<int>();
    /// generate floor seeds first
    for (int i = 0; i < 33; i++) {
      floorSeeds.Add(MyRandom.Next());
    }
    generator = new FloorGenerator(floorSeeds);
    home = new TutorialFloor();
    home.Put(player);
  }

  // private static void Analyze() {
  //   var dict = new Dictionary<Type, int[]>();
  //   for (int i = 0; i < 10; i++) {
  //     main = new GameModel(new System.Random().Next());
  //     main.generate();
  //     // Analyze(model, i, floor => floor.actors.Where((a) => a.faction == Faction.Enemy));
  //     Analyze(main, i, floor => floor.grasses);
  //   }
  //   var l = dict.ToList().OrderByDescending((pair) => pair.Value[0]);
  //   var s = String.Join("\n", l.Select((pair) => $"{pair.Key}, {String.Join(", ", pair.Value)}"));
  //   Debug.Log(s);

  //   void Analyze<T>(GameModel model, int index, Func<Floor, IEnumerable<T>> selector) {
  //     foreach (var floor in model.floors) {
  //       foreach (var actor in selector(floor)) {
  //         var t = actor.GetType();
  //         if (!dict.ContainsKey(t)) {
  //           dict.Add(t, new int[10]);
  //         }
  //         dict[t][index]++;
  //       }
  //     }
  //   }
  // }


  public void EnqueueEvent(Action cb) {
    eventQueue.Add(cb);
  }

  public void DrainEventQueue() {
    // take care - events could add more events, which then add more events
    // guard against infinite events
    int maxEventGenerations = 32;
    for (int generation = 0; generation < maxEventGenerations; generation++) {
      // clone event queue
      List<Action> queue = new List<Action>(eventQueue);

      // free up global event queue to capture new events
      eventQueue.Clear();

      // invoke actions in this generation
      for (var i = 0; i < queue.Count; i++) {
        queue[i].Invoke();
      }
      // queue.ForEach(a => a());

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

  /// depth should be either 0, cave.depth, or cave.depth + 1
  internal void PutPlayerAt(int depth) {
    Debug.Assert(depth == 0 || depth == cave.depth || depth == cave.depth + 1, "PutPlayerAt depth check");
    Floor oldFloor = player.floor;

    Serializer.SaveMainToCheckpoint();

    this.depth = depth;
    Vector2Int newPlayerPosition;
    // this could take a while
    var newFloor = depth == 0 ? home : depth == cave.depth ? cave : generator.generateCaveFloor(depth);

    // going home
    if (depth == 0) {
      newPlayerPosition = newFloor.downstairs.landing;
    } else {
      newPlayerPosition = newFloor.upstairs?.landing ?? new Vector2Int(newFloor.width / 2, newFloor.height / 2);
    }
    oldFloor.Remove(player);
    oldFloor.RecordLastStepTime(this.time);
    player.pos = newPlayerPosition;
    newFloor.CatchUpStep(this.time);
    newFloor.Put(player);
    if (newFloor.depth == cave.depth + 1) {
      cave = newFloor;
    }
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
    var enumerable = home.bodies.Where((a) => a is Plant).Cast<ISteppable>().Concat(currentFloor.steppableEntities);
    return enumerable;
  }
}
