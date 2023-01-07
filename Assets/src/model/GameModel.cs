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
  public HomeFloor home;
  public Floor cave;
  public int nextCaveDepth { get; private set; }
  public float time = 0;
  public int day = 1;
  public List<int> floorSeeds;
  public FloorGenerator generator;
  public PlayStats stats;
  public bool permadeath = true;

  private TurnManager _turnManager;
  public TurnManager turnManager {
    get {
      if (_turnManager == null) {
        _turnManager = new TurnManager(this);
      }
      return _turnManager;
    }
  }
  // if the player's dead, they probably died in the cave
  public Floor currentFloor => player.floor ?? cave;

  [field:NonSerialized] /// Controller only
  public event Action<Floor, Floor> OnPlayerChangeFloor;

  internal bool CheckObviouslyBroken() {
    if (!player.floor.InBounds(player.pos)) {
      return true;
    }
    return false;
  }

  internal void TryRecoverFromBrokenState() {
    // we should never get here, but we did. reset player back to home floor and delete the floor we just tried to generate
    PutPlayerAt(0);
    // hack force new seed and regenerate
    floorSeeds[cave.depth] = new System.Random().Next();
    cave = generator.generateCaveFloor(cave.depth);
  }

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
  }

  public GameModel() {
    seed = new System.Random().Next();
    stats = new PlayStats();
    nextCaveDepth = 1;
    #if UNITY_EDITOR
    // this.seed = 0xf380d57;
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
    // 1.10.0 added PlayStats
    if (stats == null) {
      stats = new PlayStats();
    }
  }

  internal IEnumerator StepDay(Action<Entity> OnEntityTurn, Action then) {
    yield return new WaitForSeconds(1.0f);
    day++;
    foreach (var p in home.entities.ToList()) {
      if (p is IDaySteppable s && p.isVisible) {
        try {
          OnEntityTurn(p);
          s.StepDay();
        } catch (Exception e) {
          Debug.LogError(e);
          turnManager.latestException = e;
        }
        yield return new WaitForSeconds(1f);
      }
    }
    then();
  }

  public void GoNextDay() {
    // hack route to controller for now 
    GameModelController.main.GoNextDay();
    GameModel.main.player.ReplenishActionPoints();
    // if (home is ExpandingHomeFloor expandingHomeFloor) {
    //   expandingHomeFloor.Expand();
    // }
  }

  private void generate() {
    Debug.Log("generating from seed " + seed.ToString("x"));
    MyRandom.SetSeed(seed);
    floorSeeds = new List<int>();
    /// generate floor seeds first
    for (int i = 0; i < 37; i++) {
      floorSeeds.Add(MyRandom.Next());
    }
    generator = FloorGenerator.Create(floorSeeds);
    home = generator.generateHomeFloor();
#if experimental_actionpoints
    // HACK have an empty cave floor at depth 0 so when you go down
    // you get to depth 1
    // cave = new Floor(0, 0, 0);
    GenerateAndSetCaveDepth(nextCaveDepth);
#else
    cave = generator.generateCaveFloor(1);
#endif
    player = new Player(new Vector2Int(home.root.min.x + 1, home.root.center.y));
    home.Put(player);
  }

  private void generateTutorial() {
    player = new Player(new Vector2Int(2, 4));
    // player = new Player(new Vector2Int(50, 4));
    MyRandom.SetSeed(seed);
    floorSeeds = new List<int>();
    /// generate floor seeds first
    for (int i = 0; i < 37; i++) {
      floorSeeds.Add(MyRandom.Next());
    }
    generator = new FloorGenerator200Start(floorSeeds);
    home = new TutorialFloor();
    home.Put(player);
  }

  public void GameOver(bool won, Entity deathSource = null) {
    FinalizeStats(won, deathSource?.displayName);
    PlayLog.Update(log => log.stats.Add(stats));
    OnGameOver?.Invoke(stats);
  }

  private void FinalizeStats(bool won, string killedBy = null) {
    stats.won = won;
    stats.killedBy = killedBy;
    stats.timeTaken = time;
    stats.floorsCleared = cave.depth;
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

  /// depth should be either 0, cave.depth, or cave.depth + 1
  internal void PutPlayerAt(int depth, Vector2Int? pos = null) {
    Debug.Assert(depth == 0 || depth == cave.depth || depth == cave.depth + 1, "PutPlayerAt depth check");
    Floor oldFloor = player.floor;

    // this could take a while
    Floor newFloor;
    if (depth == 0) {
      newFloor = home;
    } else if (depth == cave.depth) {
      newFloor = cave;
    } else {
      GenerateAndSetCaveDepth(depth);
      newFloor = cave;
    }

    // going home
    if (pos == null) {
        pos = newFloor.startPos;
    }
    oldFloor.RecordLastStepTime(this.time);
    newFloor.CatchUpStep(this.time);
    player.ChangeFloors(newFloor, pos.Value);
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

  internal void GenerateAndSetCaveDepth(int newDepth) {
    nextCaveDepth = newDepth;
    cave = generator.generateCaveFloor(newDepth);
  }

  /// Get all actors that should be simulated, in no particular order. This includes: 
  /// SteppableEntity's on the current floor, and
  /// Plants on any floor
  internal IEnumerable<ISteppable> GetAllEntitiesInPlay() {
#if experimental_chainfloors
    var activeRoom = player.room;
    return currentFloor.steppableEntities.Where(s => s is Entity e && e.room == activeRoom);
#else
    return currentFloor.steppableEntities;
#endif
  }
}
