using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Runtime.Serialization;

[Serializable]
public class GameModel {
  public static int MAX_ATTEMPTS = 7;
  public static string VERSION = "2.0.0";
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
  public int attempt = 1;

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

  public TutorialModel? tutorialModel;

  public static GameModel main;
  public bool canRetry => !permadeath && hasMoreAttempts;
  public bool hasMoreAttempts => attempt < MAX_ATTEMPTS;

  /// Also sets GameModel.main.
  public static void GenerateNewGameAndSetMain() {
    main = new GameModel();
    main.generate();
    main.StepUntilPlayerChoiceImmediate();
  }

  public static void GenerateTutorialAndSetMain() {
    main = new GameModel();
    main.generateTutorial();
    main.StepUntilPlayerChoiceImmediate();
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
  void HandleSerializing(StreamingContext context) {
    if (eventQueue.Any()) {
      Debug.LogError("Serializing during not null event queue! " + eventQueue.Count);
      DrainEventQueue();
    }
  }
  #endif

  [OnDeserialized]
  void HandleDeserialized(StreamingContext context) {
    eventQueue = new List<Action>();
    // 1.10.0 added PlayStats
    if (stats == null) {
      stats = new PlayStats();
    }
  }

  private void generate() {
    home = generator.generateCaveFloor(0);
    cave = generator.generateCaveFloor(1);
    player = new Player(home.startPos);
    home.Put(player);
  }

  private void generateTutorial() {
    tutorialModel = TutorialModel.createDefault();
    Prebuilt pb = Prebuilt.LoadBaked(TutorialFloor.TUTORIAL_FLOORS[0].name);

    player = pb.player;
    player.SetHPDirect(1);
    home = TutorialFloor.CreateFromPrebuilt(pb);
    home.Put(player);
    // hack to start the floor off on turn 1
    foreach (var entity in home.steppableEntities) {
      entity.timeNextAction += 1;
    }
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

  public void StepUntilPlayerChoiceImmediate() {
    var step = StepUntilPlayerChoice();
    // execute them all immediately
    do { } while (step.MoveNext());
  }

  public IEnumerator StepUntilPlayerChoice() {
    return turnManager.StepUntilPlayersChoice();
  }

  public void PutPlayerAt(Floor newFloor, Vector2Int? pos = null) {
    Floor oldFloor = player.floor;

    // going home
    if (pos == null) {
      if (newFloor.depth == 0) {
        pos = newFloor.downstairs.landing;
      } else {
        pos = newFloor.upstairs?.landing ?? newFloor.startPos;
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
    return currentFloor.steppableEntities;
  }

  // whether the current state is in a "transition" state and therefore shouldn't be saved
  public bool IsTransient() {
    return currentFloor is TutorialFloor || player.IsDead;
  }

  public void FloorCleared(Floor floor) {
    foreach (var plant in home?.bodies.OfType<Plant>().ToList()) {
      plant.OnFloorCleared(floor);
    }
    ResetAttempts();
    foreach (var handler in player.Of<IFloorClearedHandler>()) {
      handler.HandleFloorCleared(floor);
    }
  }

  private void ResetAttempts() {
    if (!hasMoreAttempts) {
      Messages.Create("Great job!");
    }
    attempt = 1;
  }

  internal static void Retry() {
    main = Serializer.LoadCheckpoint();
    if (!main.IsTransient()) {
      main.attempt++;
      Serializer.SaveMainToCheckpoint();
    }
  }
}

[Serializable]
public class HUDElement {
  public string name;
  public bool active;
  public GameObject Get => HUDController.main.GetHUDGameObject(name);
}

[Serializable]
public class TutorialModel {

  public static TutorialModel createDefault() {
    var model = new TutorialModel();

    model.HUD["depth"].active = true;
    model.HUD["waitButton"].active = true;

    return model;
  }

  private TutorialModel() {}

  public Dictionary<string, HUDElement> HUD = new Dictionary<string, HUDElement>() {
    ["depth"] = new HUDElement { name = "depth" },
    ["waitButton"] = new HUDElement { name = "waitButton" },
    ["hpBar"] = new HUDElement { name = "hpBar" },
    ["waterIndicator"] = new HUDElement { name = "waterIndicator" },
    ["inventoryToggle"] = new HUDElement { name = "inventoryToggle" },
    ["inventoryContainer"] = new HUDElement { name = "inventoryContainer" },
    ["statuses"] = new HUDElement { name = "statuses" },
    ["enemiesLeft"] = new HUDElement { name = "enemiesLeft" },
    ["settings"] = new HUDElement { name = "settings" },
    ["damageFlash"] = new HUDElement { name = "damageFlash" }
  };

  public HUDElement FindHUDElement(GameObject go) {
    foreach (var kvp in HUD) {
      if (kvp.Value.Get == go) {
        return kvp.Value;
      }
    }
    return null;
  }
}
