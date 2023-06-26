using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Runtime.Serialization;

[Serializable]
public class GameModel {
  public static string VERSION = "1.11.0";
  [OptionalField] /// added 1.10.0
  public string version = VERSION;
  public int seed;
  public Player player;
  public Floor home;
  public Floor cave;
  public int depth = 0;
  public float time = 0;
  public List<int> floorSeeds;
  public FloorGenerator generator;
  public PlayStats stats;
  public bool permadeath = false;

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
  /// controller only. Triggered when either the player dies, or when the player speaks to Ezra.
  [field:NonSerialized]
  public Action<PlayStats> OnGameOver;

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
    Serializer.SaveMainToCheckpoint();
  }

  internal static void GenerateFromPrebuiltAndSetMain(Prebuilt prebuilt) {
    main = new GameModel();
    main.initFromPrebuilt(prebuilt);
  }

  public GameModel() {
    seed = new System.Random().Next();
    stats = new PlayStats();
    #if UNITY_EDITOR
    // this.seed = 0xf380d57;
    #endif

    Debug.Log("new GameModel() - generating from seed " + seed.ToString("x"));
    MyRandom.SetSeed(seed);
    floorSeeds = new List<int>();
    /// generate floor seeds first
    for (int i = 0; i < 37; i++) {
      floorSeeds.Add(MyRandom.Next());
    }
    generator = new FloorGenerator(floorSeeds);
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
    // 1.10.0 added PlayStats
    if (stats == null) {
      stats = new PlayStats();
    }
  }

  private void generate() {
    home = generator.generateCaveFloor(0);
    cave = generator.generateCaveFloor(1);
    player = new Player(new Vector2Int(2, home.height/2));
    home.Put(player);
  }

  private void generateTutorial() {
    Prebuilt pb = Prebuilt.LoadBaked("TutorialRoom1_v2");

    player = pb.player;
    player.SetHPDirect(1);
    home = TutorialFloor1.CreateFromPrebuilt(pb);
    home.Put(player);
    DrainEventQueue();
  }

  // the only purpose of this is in-editor testing so don't worry too much about it
  private void initFromPrebuilt(Prebuilt prebuilt) {
    player = prebuilt.player;
    cave = prebuilt.createRepresentativeFloor();
    // HACK
    home = cave;
    PutPlayerAt(cave, player.pos);
  }


  public void GameOver(bool won, Entity deathSource = null) {
    FinalizeStats(won, deathSource?.displayName);
    PlayLog.Update(log => log.stats.Add(stats));
    OnGameOver?.Invoke(stats);
  }

  private void FinalizeStats(bool won, string killedBy = null) {
    stats.won = won;
    stats.killedBy = killedBy;
    stats.timeTaken = GameModel.main.time;
    stats.floorsCleared = GameModel.main.cave?.depth ?? 0;
  }

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

  public void PutPlayerAt(Floor newFloor, Vector2Int? pos = null) {
    Floor oldFloor = player.floor;

    // going home
    if (pos == null) {
      if (depth == 0) {
        pos = newFloor.downstairs.landing;
      } else {
        pos = newFloor.upstairs?.landing ?? new Vector2Int(2, newFloor.height / 2);
      }
    }
    newFloor.CatchUpStep(this.time);
    this.depth = newFloor.depth;
    player.ChangeFloors(newFloor, pos.Value);
    if (depth != 0) {
      cave = newFloor;
    }
    OnPlayerChangeFloor?.Invoke(oldFloor, newFloor);
  }

  /// depth should be either 0, cave.depth, or cave.depth + 1
  public void PutPlayerAt(int depth, Vector2Int? pos = null) {
    Debug.Assert(depth == 0 || depth == cave.depth || depth == cave.depth + 1, "PutPlayerAt depth check");
    // this could take a while
    var newFloor = depth == 0 ? home : depth == cave.depth ? cave : generator.generateCaveFloor(depth);
    PutPlayerAt(newFloor, pos);
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
