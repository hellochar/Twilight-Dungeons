using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using Random = System.Random;

[Serializable]
public class FloorGenerator {
  public EncounterGroupShared shared;
  public EncounterGroup EncounterGroup;
  public List<int> floorSeeds;
  public EncounterGroup earlyGame, everything, midGame;

  [NonSerialized] /// these are hard-coded and reinstantiated when the program runs
  public List<Func<Floor>> floorGenerators;

  [OnDeserialized]
  void HandleDeserialized() {
    InitFloorGenerators();
    // 1.10.0 32 -> 36 levels
    while (floorSeeds.Count() < floorGenerators.Count()) {
      floorSeeds.Add(MyRandom.Next());
    }
  }

  public FloorGenerator(List<int> floorSeeds) {
    this.floorSeeds = floorSeeds;
    shared = new EncounterGroupShared();
    earlyGame = EncounterGroup.EarlyGame().AssignShared(shared);
    everything = EncounterGroup.EarlyMidMixed().AssignShared(shared);
    midGame = EncounterGroup.MidGame().AssignShared(shared);
    InitFloorGenerators();
  }

  private void InitFloorGenerators() {
    floorGenerators = new List<Func<Floor>>() {
      // early game
      () => generateFloor0(0),
      () => generateSingleRoomFloor(1, 9, 6, 1, 1),
      () => generateSingleRoomFloor(2, 9, 6, 1, 1, extraEncounters: Encounters.OneAstoria),
      () => generateSingleRoomFloor(3, 9, 7, 2, 1, extraEncounters: Encounters.AddOneWater),
      () => generateSingleRoomFloor(4, 9, 7, 2, 1, true, extraEncounters: Encounters.OneAstoria),
      () => generateSingleRoomFloor(5, 10, 8, 3, 2),
      () => generateSingleRoomFloor(6, 10, 8, 3, 2, extraEncounters: Encounters.AddOneWater),
      () => generateSingleRoomFloor(7, 10, 8, 3, 3),
      () => generateRewardFloor(8, shared.Plants.GetRandomAndDiscount(1f), Encounters.AddDownstairsInRoomCenter),
      () => generateBlobBossFloor(9),

      // midgame
      () => generateSingleRoomFloor(10, 9, 7, 2, 1),
      () => generateSingleRoomFloor(11, 9, 7, 2, 1, extraEncounters: Encounters.AddOneWater),
      () => generateSingleRoomFloor(12, 10, 8, 3, 1),
      () => generateSingleRoomFloor(13, 10, 8, 3, 2, true, extraEncounters: Encounters.OneAstoria),
      () => generateSingleRoomFloor(14, 11, 8, 4, 2),
      () => generateSingleRoomFloor(15, 12, 8, 4, 3),
      () => generateSingleRoomFloor(16, 12, 8, 5, 5),
      () => generateRewardFloor(17, Encounters.AddDownstairsInRoomCenter, Encounters.FungalColonyAnticipation, shared.Plants.GetRandomAndDiscount(1f)),
      () => generateFungalColonyBossFloor(18),

      // endgame
      () => generateSingleRoomFloor(19, 10, 8, 2, 2),
      () => generateSingleRoomFloor(20, 12, 8, 3, 2),
      () => generateSingleRoomFloor(21, 14, 9, 4, 3, true, new Encounter[] { Encounters.LineWithOpening, Encounters.ChasmsAwayFromWalls1 }),
      () => generateSingleRoomFloor(22, 13, 9, 5, 2),
      () => generateSingleRoomFloor(23, 14, 9, 6, 4),
      () => generateSingleRoomFloor(24, 14, 9, 7, 4, extraEncounters: Encounters.AddWater),
      () => generateSingleRoomFloor(25, 14, 9, 8, 5),
      () => generateSingleRoomFloor(26, 14, 9, 9, 6),
      () => generateEndFloor(27),
    };
  }

  public Floor generateCaveFloor(int depth) {
    /// The generators rely on the following state:
    /// (1) encounter group
    /// (2) MyRandom seed

    /// configure the EncounterGroup
    if (depth <= 9) {
      EncounterGroup = earlyGame;
    } else if (depth <= 18) {
      EncounterGroup = everything;
    } else {
      EncounterGroup = midGame;
    }

    /// set the seed
    Debug.Log("Depth " + depth + " seed " + floorSeeds[depth].ToString("x"));
    MyRandom.SetSeed(floorSeeds[depth]);

    // pick the generator
    var generator = floorGenerators[depth];
    Floor floor = null;

    int guard = 0;
    while (floor == null && guard++ < 20) {
      #if !UNITY_EDITOR
      try {
        floor = generator();
      } catch (Exception e) {
        Debug.LogError(e);
        GameModel.main.turnManager.latestException = e;
        MyRandom.Next();
      }
      #else
      floor = generator();
      #endif
    }
    if (floor == null) {
      throw GameModel.main.turnManager.latestException;
    }
    if (floor.depth != depth) {
      throw new Exception("floorGenerator depth " + depth + " is marked as depth " + floor.depth);
    }

    PostProcessFloor(floor);

    return floor;
  }

  public void PostProcessFloor(Floor floor) {
    floor.CheckCleared();
    // PostProcessPushEnemiesBack(floor);

    // PostProcessReduceEnemyAndGrassCount(floor);

#if UNITY_EDITOR
    var depth = floor.depth;
    floor.depth = 20;
    // put stuff here
    // Encounters.OneButterfly(floor, floor.root);
    // Encounters.AddSoftMoss(floor, floor.root);
    // Encounters.AddBird(floor, floor.root);
    // Encounters.AddSnake(floor, floor.root);
    // Encounters.AddScuttlers(floor, floor.root);
    // Encounters.AddBloodwort(floor, floor.root);
    // Encounters.AddShielders(floor, floor.root);
    // Encounters.AddWallflowers(floor, floor.root);
    floor.depth = depth;
#endif

    PostProcessAddSignpost(floor);
  }

  private static void PostProcessAddSignpost(Floor floor) {
    /// add a signpost onto the floor
    if (Tips.tipMap.ContainsKey(floor.depth)) {
      /// put it near the upstairs
      var signpostSearchStartPos = floor.startPos;
      var signpostPos = floor.BreadthFirstSearch(signpostSearchStartPos, (tile) => true).Skip(5).Where(t => t is Ground && t.CanBeOccupied() && t.grass == null).FirstOrDefault();
      if (signpostPos != null) {
        floor.Put(new Signpost(signpostPos.pos, Tips.tipMap[floor.depth]));
      }
    }
  }

  public void PostProcessReduceEnemyAndGrassCount(Floor floor, float amountScalar = 0.67f) {
    // shrinking hack; reduce number of bodies and grasses
    var enemies = floor.Enemies().Where(a => !(a is Boss)).ToList();
    var originalEnemyNum = enemies.Count();
    var newEnemyNum = Mathf.Max(1, Mathf.RoundToInt(enemies.Count() * amountScalar));
    while(enemies.Count > newEnemyNum) {
      var choice = Util.RandomPick(enemies);
      enemies.Remove(choice);
      floor.Remove(choice);
    }

    var nonEnemyBodies = floor.bodies.Except(enemies).ToList();
    var originalNonEnemyBodyNum = nonEnemyBodies.Count();
    var newNonEnemyBodyNum = Mathf.Max(1, Mathf.RoundToInt(nonEnemyBodies.Count() * amountScalar));
    while (nonEnemyBodies.Count > newNonEnemyBodyNum) {
      var choice = Util.RandomPick(nonEnemyBodies);
      nonEnemyBodies.Remove(choice);
      floor.Remove(choice);
    }

    var grasses = floor.grasses.ToList();
    var originalGrassesNum = grasses.Count();
    var newGrassNum = Mathf.Max(1, Mathf.RoundToInt(grasses.Count() * 0.5f));
    while(grasses.Count > newGrassNum) {
      var choice = Util.RandomPick(grasses);
      grasses.Remove(choice);
      floor.Remove(choice);
    }

    Debug.LogFormat("Enemies {0} -> {1}, non-enemy bodies {2} -> {3}, grasses {4} -> {5}",
    originalEnemyNum, newEnemyNum,
    originalNonEnemyBodyNum, newNonEnemyBodyNum,
    originalGrassesNum, newGrassNum
    );
  }

  void PostProcessPushEnemiesBack(Floor floor) {
    // push all enemies back so the player has a few turns to "prepare"
    var enemies = floor.Enemies().Where(a => !(a is Boss)).ToList();
    foreach(var enemy in enemies) {
      var canOccupyMethod = enemy.GetType().GetMethod("CanOccupy", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
      bool canOccupy(Tile t) {
        if (!t.CanBeOccupied()) {
          return false;
        }
        if (canOccupyMethod != null) {
          return (bool) canOccupyMethod.Invoke(null, new object[] { t });
        }
        return true;
      }
      var newTile = floor
        .EnumerateFloor()
        .Select(pos => floor.tiles[pos])
        .Where(canOccupy)
        .OrderByDescending(t => t.pos.x)
        .Skip(MyRandom.Range(0, 10))
        .FirstOrDefault();
      // var xRel = enemy.pos.x - enemyRoom.min.x;
      // // var pushBackChance = Util.MapLinear(xRel, 1, enemyRoom.width - 1, 0.9f, 0.0f);
      // var pushBackChance = 1;
      // if (MyRandom.value < pushBackChance) {
      //   List<Tile> possibleLaterTiles = new List<Tile>();
      //   var xPlusOneRel = xRel + 1;
      //   var x1 = xPlusOneRel + enemyRoom.min.x;
      //   for(int y = 0; y < floor.height; y++) {
      //     if (floor.tiles[x1, y].CanBeOccupied()) {
      //       possibleLaterTiles.Add(floor.tiles[x1, y]);
      //     }
      //   }
      //   var newTile = Util.RandomPick(possibleLaterTiles);
        if (newTile != null) {
          // enemy.ForceSet(newTile.pos);
          enemy.pos = newTile.pos;
        }
      // }
    }
  }

    public Floor generateEndFloor(int depth) {
    // use bossfloor to get rid of the sound
    Floor floor = new BossFloor(depth, 14, 42);

    FloorUtils.SurroundWithWalls(floor);

    var room0 = new Room(floor);
    floor.rooms = new List<Room> { room0 };
    floor.root = room0;

    var treePos = room0.center + Vector2Int.up * 10;

    // carve out chasms/ground
    for (int y = 0; y < floor.height; y++) {
      var angle = Util.MapLinear(y, 0, floor.height / 2, 0, Mathf.PI / 2 * 1.7f);
      var width = Mathf.Max(0, Mathf.Cos(angle)) * (floor.width - 2f) + 1;
      var xMin = (floor.width / 2f - width / 2) - 1;
      var xMax = (xMin + width);
      for (int x = 0; x < floor.width; x++) {
        var pos = new Vector2Int(x, y);
        if (x < xMin || x > xMax || y > treePos.y) {
          floor.Put(new Chasm(pos));
        } else if (floor.tiles[pos] == null) {
          floor.Put(new Ground(pos));
        }
      }
    }

    floor.PlaceUpstairs(new Vector2Int(floor.width / 2 - 1, 1), false);

    Room roomBot = new Room(Vector2Int.one, new Vector2Int(floor.width - 2, 11));
    for (var i = 0; i < 1; i++) {
      Encounters.AddWater(floor, roomBot);
    }
    Encounters.TwelveRandomAstoria(floor, roomBot);
    for (var i = 0; i < 12; i++) {
      Encounters.AddGuardleaf(floor, roomBot);
    }

    foreach (var t in floor.EnumerateRoom(roomBot).Where(p => floor.tiles[p] is Ground && floor.grasses[p] == null)) {
      floor.Put(new SoftGrass(t));
    }

    foreach (var pos in floor.EnumerateCircle(treePos + Vector2Int.down, 2.5f).Where(p => p.y <= treePos.y)) {
      var grass = floor.grasses[pos];
      if (grass != null) {
        floor.Remove(grass);
      }
      floor.Put(new FancyGround(pos));
    }

    /// add Tree of Life right in the middle
    floor.Put(new TreeOfLife(treePos));
    floor.Put(new Ezra(treePos + Vector2Int.down * 2));

    return floor;
  }

  // A little dose of coziness, a half-reward, something nice, a breather
  // maybe a pool of water to wash off on, a place to rest and recover
  // maybe healing
  // 
  public Floor generateRestFloor(int depth) {
    Floor floor = new Floor(depth, 8 + 2, 6 + 2);
    FloorUtils.CarveGround(floor);
    FloorUtils.SurroundWithWalls(floor);
    FloorUtils.NaturalizeEdges(floor);

    var room0 = new Room(floor);

    floor.PlaceUpstairs(new Vector2Int(room0.min.x, room0.max.y));
    floor.PlaceDownstairs(new Vector2Int(room0.max.x, room0.min.y));

    Encounters.AddOneWater(floor, room0);
    shared.Rests.GetRandomAndDiscount(0.5f)(floor, room0);

    FloorUtils.TidyUpAroundStairs(floor);
    floor.root = room0;
    return floor;
  }

  public Floor generateFloor0(int depth) {
    Floor floor = new Floor(depth, 12, 7).StartCleared();

    // fill with floor tiles by default
    FloorUtils.CarveGround(floor);

    FloorUtils.SurroundWithWalls(floor);
    // FloorUtils.NaturalizeEdges(floor);

    // floor.PlaceUpstairs(new Vector2Int(1, floor.height / 2));

    var soils = new List<Soil>();
    for (int x = 2; x < floor.width - 2; x += 2) {
      int y = floor.height / 2 - 1;
      if (floor.tiles[x, y] is Ground) {
        soils.Add(new Soil(new Vector2Int(x, y)));
      }
      y = floor.height / 2 + 1;
      if (floor.tiles[x, y] is Ground) {
        soils.Add(new Soil(new Vector2Int(x, y)));
      }
    }
    floor.PutAll(soils);

    var room0 = new Room(floor);
    floor.rooms = new List<Room> { room0 };
    floor.root = room0;

    if (GardenTutorialController.ShouldShow) {
      Encounters.MatureBerryBush(floor, room0);
      EncounterGroup.Plants.Remove(Encounters.MatureBerryBush);
      // in tutorial mode, the downstairs will be placed once the tutorial finishes
    } else {
      EncounterGroup.Plants.GetRandomAndDiscount(1f)(floor, room0);
      floor.PlaceDownstairs(new Vector2Int(floor.width - 1, floor.height / 2));
      var altarPos = floor.downstairsPos + new Vector2Int(-1, 1);
      floor.Put(new Ground(altarPos));
      floor.Put(new Altar(altarPos));
    }

    Encounters.AddWater(floor, room0);
    // Encounters.ThreeAstoriasInCorner(floor, room0);

    #if UNITY_EDITOR
    Encounters.MatureFaeleaf(floor, room0);
    floor.depth = 20;
    // Encounters.AddOctopus(floor, room0);
    // Encounters.AddSkulls(floor, room0);
    // Encounters.AddIronJelly(floor, room0);
    // Encounters.AddHoppers(floor, room0);
    // Encounters.AddDeathbloom(floor, room0);
    // Encounters.AddMushroom(floor, room0);
    // Encounters.AddGrasper(floor, room0);
    // Encounters.AddHydra(floor, room0);
    // Encounters.AddViolets(floor, room0);
    // Encounters.AddTunnelroot(floor, room0);
    // Encounters.AddWildekins(floor, room0);
    // Encounters.AddCrabs(floor, room0);
    // Encounters.AddParasite(floor, room0);
    // Encounters.AddCoralmoss(floor, room0);
    // Encounters.AddHangingVines(floor, room0);
    // Encounters.AddEveningBells(floor, room0);
    // Encounters.OneButterfly(floor, room0);
    // Encounters.AddSpiders(floor, room0);
    // Encounters.AddSpore(floor, room0);
    // Encounters.AddBladegrass(floor, room0);
    // Encounters.ScatteredBoombugs(floor, room0);
    // Encounters.AddBats(floor, room0);
    // Encounters.AddPumpkin(floor, room0);
    // Encounters.ScatteredBoombugs.Apply(floor, room0);
    // Encounters.AFewSnails(floor, room0);
    // Encounters.AFewBlobs(floor, room0);
    floor.depth = 0;
    #endif

    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }

  public Floor generateRewardFloor(int depth, params Encounter[] extraEncounters) {
    Floor floor = new Floor(depth, 12, 8);
    FloorUtils.CarveGround(floor);
    FloorUtils.SurroundWithWalls(floor);
    FloorUtils.NaturalizeEdges(floor);

    var room0 = new Room(floor);

    floor.PlaceUpstairs(new Vector2Int(room0.min.x, room0.max.y));
    // floor.PlaceDownstairs(new Vector2Int(room0.max.x, room0.min.y));

    // Encounters.PlaceFancyGround(floor, room0);
    // Encounters.CavesRewards.GetRandomAndDiscount()(floor, room0);
    // EncounterGroup.Plants.GetRandomAndDiscount(0.9f)(floor, room0);
    // Encounters.AddTeleportStone(floor, room0);
    Encounters.AddOneWater(floor, room0);
    foreach (var encounter in extraEncounters) {
      encounter(floor, room0);
    }

    floor.PlaceDownstairs(floor.downstairsPos);

    FloorUtils.TidyUpAroundStairs(floor);
    floor.root = room0;

    return floor;
  }

  /// <summary>
  /// Generates one single room with one wall variation, X mob encounters, Y grass encounters, an optional reward.
  /// Good for a contained experience.
  /// </summary>
  public Floor generateSingleRoomFloor(int depth, int width, int height, int numMobs, int numGrasses, bool reward = false, Encounter[] preMobEncounters = null, params Encounter[] extraEncounters) {
    Floor floor = tryGenerateSingleRoomFloor(depth, width, height, preMobEncounters == null);
    floor.PutAll(
      floor.EnumeratePerimeter().Where(pos => floor.tiles[pos] is Ground).Select(pos => new Wall(pos))
    );

    var room0 = floor.root;
    if (preMobEncounters != null) {
      foreach (var encounter in preMobEncounters) {
        encounter(floor, room0);
      }
    }
    // X mobs
    for (var i = 0; i < numMobs; i++) {
      EncounterGroup.Mobs.GetRandomAndDiscount()(floor, room0);
    }
    EncounterGroup.Spice.GetRandom()(floor, room0);
    // PostProcessPushEnemiesBack(floor);

    // Y grasses
    for (var i = 0; i < numGrasses; i++) {
      EncounterGroup.Grasses.GetRandomAndDiscount()(floor, room0);
    }

    foreach (var encounter in extraEncounters) {
      encounter(floor, room0);
    }

    // a reward (optional)
    if (reward) {
      Encounters.AddWater(floor, room0);
      EncounterGroup.Rewards.GetRandomAndDiscount()(floor, room0);
    }

    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }

  private Floor tryGenerateSingleRoomFloor(int depth, int width, int height, bool defaultEncounters = true) {
    Floor floor = new Floor(depth, width, height);
    floor.startPos = new Vector2Int(1, floor.height / 2);

    // fill with wall
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Wall(p));
    }

    Room room0 = new Room(floor);
    FloorUtils.CarveGround(floor, floor.EnumerateRoom(room0));

    if (defaultEncounters) {
      // one wall variation
      EncounterGroup.Walls.GetRandomAndDiscount()(floor, room0);
      FloorUtils.NaturalizeEdges(floor);
      
      // chasms (bridge levels) should be relatively rare so only discount by 10% each time (this is still exponential decrease for the Empty case)
      EncounterGroup.Chasms.GetRandomAndDiscount(0.04f)(floor, room0);
    }

    ensureConnectedness(floor);

    // floor.PlaceUpstairs(new Vector2Int(0, floor.height / 2));
    // floor.PlaceDownstairs(new Vector2Int(width - 1, floor.height / 2));

    floor.root = room0;
    floor.rooms = new List<Room>();
    floor.upstairsRoom = room0;
    floor.downstairsRoom = room0;

    return floor;
  }

  public Floor generateBlobBossFloor(int depth) {
    Floor floor = new BossFloor(depth, 14, 9);
    // fill with wall
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Ground(p));
    }
    foreach(var p in floor.EnumeratePerimeter()) {
      floor.Put(new Wall(p));
    }
    floor.Put(new Wall(new Vector2Int(1, 1)));
    floor.Put(new Wall(new Vector2Int(floor.width - 2, 1)));
    floor.Put(new Wall(new Vector2Int(floor.width - 2, floor.height - 2)));
    floor.Put(new Wall(new Vector2Int(1, floor.height - 2)));

    Room room0 = new Room(floor);
    floor.Put(new Wall(room0.center + new Vector2Int(2, 2)));
    floor.Put(new Wall(room0.center + new Vector2Int(2, -2)));
    floor.Put(new Wall(room0.center + new Vector2Int(-2, -2)));
    floor.Put(new Wall(room0.center + new Vector2Int(-2, 2)));

    EncounterGroup.Grasses.GetRandomAndDiscount()(floor, room0);

    // floor.PlaceUpstairs(new Vector2Int(1, floor.height / 2));
    // floor.PlaceDownstairs(new Vector2Int(floor.width - 2, floor.height / 2));

    floor.root = room0;
    floor.rooms = new List<Room>();
    floor.upstairsRoom = room0;
    floor.downstairsRoom = room0;

    // add boss
    floor.Put(new Blobmother(new Vector2Int(floor.width - 2, floor.height / 2)));


    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }

  public Floor generateFungalColonyBossFloor(int depth) {
    Floor floor = new BossFloor(depth, 13, 9);
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Wall(p));
    }

    Room room0 = new Room(floor);

    void CutOutCircle(Vector2Int center, float radius) {
      FloorUtils.CarveGround(floor, floor.EnumerateCircle(center, radius));
    }

    // start and end paths
    CutOutCircle(new Vector2Int(3, floor.height / 2), 2f);
    CutOutCircle(new Vector2Int(floor.width - 4, floor.height / 2), 2f);
    CutOutCircle(room0.center, 4.5f);
    floor.Put(new FungalColony(room0.center));

    // turn some of the walls and empty space into fungal walls
    foreach (var pos in floor.EnumerateFloor()) {
      var t = floor.tiles[pos];
      if (t is Wall && floor.GetAdjacentTiles(t.pos).Any(t2 => t2.CanBeOccupied())) {
        floor.Put(new FungalWall(pos));
      } else if (t is Ground && t.CanBeOccupied()) {
        if (MyRandom.value < 0.25f) {
          floor.Put(new FungalWall(pos));
        }
      }
    }

    // re-apply normal Wall to outer perimeter
    floor.PutAll(floor.EnumeratePerimeter().Select(pos => new Wall(pos)));

    // block entrance
    // floor.PutAll(new FungalWall(new Vector2Int(7, 5)), new FungalWall(new Vector2Int(7, 6)), new FungalWall(new Vector2Int(7, 7)));
    // floor.PlaceUpstairs(new Vector2Int(0, floor.height / 2));
    // floor.PlaceDownstairs(new Vector2Int(floor.width - 1, floor.height / 2));

    floor.root = room0;
    floor.rooms = new List<Room>();
    floor.upstairsRoom = room0;
    floor.downstairsRoom = room0;

    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }

  /// <summary>
  /// Generate a floor broken up into X smaller rooms, based on a number of "splits". Each room contains:
  /// one mob, one grass, one random encounter.
  /// </summary>
  public Floor generateMultiRoomFloor(int depth, int width = 60, int height = 20, int numSplits = 20, bool hasReward = false, params Encounter[] specialDownstairsEncounters) {
    Floor floor = tryGenerateMultiRoomFloor(depth, width, height, numSplits);
    ensureConnectedness(floor);

    var intermediateRooms = floor.rooms
      .Where((room) => room != floor.upstairsRoom && room != floor.downstairsRoom);

    Room rewardRoom = null;
    if (hasReward) {
      // the non-downstairs terminal room farthest away from the upstairs according to pathfinding
      rewardRoom = intermediateRooms
        .OrderByDescending((room) => floor.FindPath(floor.upstairs.pos, room.center).Count)
        .First();

      Encounters.PlaceFancyGround(floor, rewardRoom);
      Encounters.SurroundWithRubble(floor, rewardRoom);
      var rewardEncounter = EncounterGroup.Rewards.GetRandomAndDiscount();
      rewardEncounter(floor, rewardRoom);
    }

    var deadEndRooms = intermediateRooms.Where((room) => room != rewardRoom && room.connections.Count < 2);
    foreach (var room in deadEndRooms) {
      if (MyRandom.value < 0.05f) {
        Encounters.SurroundWithRubble(floor, room);
      }
      var encounter = EncounterGroup.Spice.GetRandomAndDiscount();
      encounter(floor, room);
    }

    foreach (var room in floor.rooms) {
      if (room != floor.upstairsRoom && room != rewardRoom) {
        // spawn a random encounter
        var encounter = EncounterGroup.Mobs.GetRandomAndDiscount();
        encounter(floor, room);
      }
    }

    // this includes abstract rooms!
    foreach(var room in floor.root.Traverse().Where((room) => room.depth >= 2)) {
      var encounter = EncounterGroup.Grasses.GetRandomAndDiscount();
      encounter(floor, room);
    }

    foreach (var encounter in specialDownstairsEncounters) {
      encounter(floor, floor.downstairsRoom);
    }

    FloorUtils.TidyUpAroundStairs(floor);

    return floor;
  }

  private static void ensureConnectedness(Floor floor) {
    // here's how we ensure connectedness:
    // 1. find all walkable tiles on a floor
    // 2. group them by connectedness using bfs, starting from the upstairs as the "mainland"
    // 3. *connect* every group past mainland, updating mainland as you go
    var walkableTiles = new HashSet<Tile>(floor
      .EnumerateFloor()
      .Select(p => floor.tiles[p])
      .Where(t => t.BasePathfindingWeight() != 0)
    );
    
    // var mainland = makeGroup(floor.upstairs, ref walkableTiles);
    var mainland = makeGroup(floor.tiles[floor.startPos], ref walkableTiles);
    int guard = 0;
    while (walkableTiles.Any() && (guard++ < 99)) {
      var island = makeGroup(walkableTiles.First(), ref walkableTiles);
      // connect island to mainland
      var entitiesToAdd = connectGroups(mainland, island);
      floor.PutAll(entitiesToAdd);
      mainland.UnionWith(entitiesToAdd);
      mainland.UnionWith(island);
    }

    // mutates walkableTiles
    HashSet<Tile> makeGroup(Tile start, ref HashSet<Tile> allTiles) {
      var set = new HashSet<Tile>(floor.BreadthFirstSearch(start.pos, allTiles.Contains));
      allTiles.ExceptWith(set);
      return set;
    }

    // How to "connect" two groups? Two groups will be separated by at least one unwalkable
    // tile. There's many ways to connect two groups. We want to choose the least invasive.
    // 
    // Bfs with distance outwards from group2 until you hit a group1 tile. Backtrace the bfs - 
    // this will give you a line of at least 3 tiles: [group2 tile, <intermediate unwalkable tiles>, group1 tile]
    // Now we transform these. Easiest is to just turn unwalkables into ground.
    // We can also get fancy by changing wall -> ground+rock and chasm -> bridge
    // We can put an "activatable" on either side that spawns a bridge.
    // another option: just *randomly* select two nodes and draw a line between them
    List<Ground> connectGroups(HashSet<Tile> mainland, HashSet<Tile> island) {
      var mainlandNode = Util.RandomPick(mainland);
      var islandNode = Util.RandomPick(island);
      // return floor.EnumerateLine(mainlandNode.pos, islandNode.pos)
      return FloorUtils.Line3x3(floor, mainlandNode.pos, islandNode.pos)
        .Where(p => floor.tiles[p].BasePathfindingWeight() == 0)
        .Select(p => new Ground(p))
        .ToList();
    }
  }

  /// connectivity is not guaranteed
  private static Floor tryGenerateMultiRoomFloor(int depth, int width, int height, int numSplits) {
    Floor floor = new Floor(depth, width, height);

    // fill with wall
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Wall(p));
    }

    // randomly partition space into rooms
    Room root = new Room(floor);
    for (int i = 0; i < numSplits; i++) {
      bool success = root.randomlySplit();
      if (!success) {
        Debug.LogWarning("couldn't split at iteration " + i);
        break;
      }
    }

    // collect all rooms
    List<Room> rooms = root.Traverse().Where(n => n.isTerminal).ToList();

    // shrink it into a subset of the space available; adds more 'emptiness' to allow
    // for less rectangular shapes
    rooms.ForEach(room => room.randomlyShrink());

    // sort rooms by distance to top-left, where the upstairs will be.
    Vector2Int topLeft = new Vector2Int(0, floor.height);
    rooms.OrderBy(room => Util.manhattanDistance(room.getTopLeft() - topLeft));

    Room upstairsRoom = rooms.First();
    // 1-px padding from the top-left of the room
    Vector2Int upstairsPos = new Vector2Int(upstairsRoom.min.x + 1, upstairsRoom.max.y - 1);
    floor.PlaceUpstairs(upstairsPos);

    Room downstairsRoom = rooms.Last();
    // 1-px padding from the bottom-right of the room
    Vector2Int downstairsPos = new Vector2Int(downstairsRoom.max.x - 1, downstairsRoom.min.y + 1);
    floor.PlaceDownstairs(downstairsPos);

    floor.root = root;
    floor.rooms = rooms;
    floor.upstairsRoom = upstairsRoom;
    floor.downstairsRoom = downstairsRoom;

    // fill each room with floor
    rooms.ForEach(room => {
      FloorUtils.CarveGround(floor, floor.EnumerateRoom(room));
    });

    // in rare cases, connect rooms using the connectedness algorithm. creates really dense and tight rooms.
    var useConnectednessAlgorithm = MyRandom.value < 0.2f;
    if (useConnectednessAlgorithm) {
      ensureConnectedness(floor);
    } else {
      // add connections between bsp siblings
      foreach (var (a, b) in ComputeRoomConnections(rooms, root)) {
        FloorUtils.CarveGround(floor, FloorUtils.Line3x3(floor, a, b));
      }
    }

    FloorUtils.NaturalizeEdges(floor);

    // occasionally create a chasm in the multi room. Feels tight and cool, and makes the level harder.
    if (MyRandom.value < 0.1) {
      var depth2Room = Util.RandomPick(root.Traverse().Where(r => r.depth == 2));
      Encounters.ChasmsAwayFromWalls1(floor, depth2Room);
    }

    return floor;
  }

  /// Connect all the rooms together with at least one through-path
  private static List<(Vector2Int, Vector2Int)> ComputeRoomConnections(List<Room> rooms, Room root) {
    return BSPSiblingRoomConnections(rooms, root);
  }

  /// draw a path connecting siblings together, including intermediary nodes (guarantees connectedness)
  /// this tends to draw long lines that cut right through single thickness walls
  private static List<(Vector2Int, Vector2Int)> BSPSiblingRoomConnections(List<Room> rooms, Room root) {
    List<(Vector2Int, Vector2Int)> paths = new List<(Vector2Int, Vector2Int)>();
    foreach (var node in root.Traverse().Where(n => !n.isTerminal)) {
      Vector2Int nodeCenter = node.getCenter();
      RoomSplit split = node.split.Value;
      split.a.connections.Add(split.b);
      split.b.connections.Add(split.a);
      Vector2Int aCenter = split.a.getCenter();
      Vector2Int bCenter = split.b.getCenter();
      paths.Add((nodeCenter, aCenter));
      paths.Add((nodeCenter, bCenter));
    }
    return paths;
  }

  public static bool AreStairsConnected(Floor floor) {
    var path = floor.FindPath(floor.downstairs.pos, floor.upstairs.pos);
    return path.Any();
  }
}
