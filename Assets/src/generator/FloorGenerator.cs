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
  private EncounterGroup earlyGame, everything, midGame;

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
      () => generateFloor0(0),
      () => generateSingleRoomFloor(1, 11, 11),
      () => generateSingleRoomFloor(2, 10, 10, extraEncounters: Encounters.OneAstoria),
      () => generateSingleRoomFloor(3, 10, 10),
      () => generateSingleRoomFloor(4, 11, 11, 1, 1, true, extraEncounters: Encounters.OneAstoria),
      () => generateSingleRoomFloor(5, 15, 11, 2),
      () => generateSingleRoomFloor(6, 13, 11, 2),
      () => generateSingleRoomFloor(7, 11, 11, 2),
      () => generateRewardFloor(8, shared.Plants.GetRandomAndDiscount(1f), Encounters.OneAstoria),
      () => generateSingleRoomFloor(9, 13, 9, 2, 2),
      () => generateSingleRoomFloor(10, 14, 7, 2, 2),
      () => generateSingleRoomFloor(11, 20, 9, 3, 2, true, null, Encounters.AddDownstairsInRoomCenter),
      () => generateBlobBossFloor(12),
      () => generateSingleRoomFloor(13, 12, 12, 4, 3),
      () => generateSingleRoomFloor(14, 15, 11, 4, 3),
      () => generateSingleRoomFloor(15, 20, 9, 5, 3),
      () => generateRewardFloor(16, shared.Plants.GetRandomAndDiscount(1f), Encounters.OneAstoria),
      () => generateMultiRoomFloor(17, 30, 20, 7),
      () => generateMultiRoomFloor(18, 20, 20, 8),
      () => generateMultiRoomFloor(19, 24, 16, 9, true),
      () => generateMultiRoomFloor(20, 30, 12, 10),
      () => generateMultiRoomFloor(21, 30, 20, 15),
      () => generateMultiRoomFloor(22, 40, 20, 20, true, Encounters.AddDownstairsInRoomCenter, Encounters.FungalColonyAnticipation),
      () => generateFungalColonyBossFloor(23),
      () => generateRewardFloor(24, shared.Plants.GetRandomAndDiscount(1f), Encounters.AddWater),
      () => generateSingleRoomFloor(25, 11, 11, 2, 2),
      () => generateSingleRoomFloor(26, 15, 15, 3, 3),
      () => generateSingleRoomFloor(27, 19, 14, 4, 3, false, null, Encounters.AddWater),
      () => generateSingleRoomFloor(28, 23, 17, 8, 6, false, new Encounter[] { Encounters.LineWithOpening, Encounters.ChasmsAwayFromWalls2 }),
      () => generateMultiRoomFloor(29, 24, 20, 10),
      () => generateMultiRoomFloor(30, 24, 24, 12, true),
      () => generateMultiRoomFloor(31, 24, 16, 12),
      () => generateRewardFloor(32, shared.Plants.GetRandomAndDiscount(1f), Encounters.AddWater, Encounters.ThreeAstoriasInCorner),
      () => generateMultiRoomFloor(33, 26, 17, 15, true),
      () => generateMultiRoomFloor(34, 28, 19, 20),
      () => generateMultiRoomFloor(35, 45, 25, 30),
      () => generateEndFloor(36),
    };
  }

  public Floor generateCaveFloor(int depth) {
    /// The generators rely on the following state:
    /// (1) encounter group
    /// (2) MyRandom seed

    /// configure the EncounterGroup
    if (depth <= 12) {
      EncounterGroup = earlyGame;
    } else if (depth <= 24) {
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

    #if UNITY_EDITOR
    // Encounters.AddFruitingBodies(floor, floor.root);
    // Encounters.AddNecroroot(floor, floor.root);
    // Encounters.AddPoisonmoss(floor, floor.root);
    // Encounters.FillWithFerns(floor, floor.root);
    // Encounters.AddFakeWall(floor, floor.root);
    #endif

    /// add a signpost onto the floor
    if (Tips.tipMap.ContainsKey(floor.depth)) {
      /// put it near the upstairs
      var signpostSearchStartPos = floor.upstairs?.landing ?? new Vector2Int(3, floor.height / 2);
      var signpostPos = floor.BreadthFirstSearch(signpostSearchStartPos, (tile) => true).Skip(5).Where(t => t is Ground && t.CanBeOccupied() && t.grass == null).FirstOrDefault();
      if (signpostPos != null) {
        floor.Put(new Signpost(signpostPos.pos, Tips.tipMap[floor.depth]));
      }
    }
    return floor;
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
    Floor floor = new Floor(depth, 16, 12);
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
    Floor floor = new Floor(depth, 18, 14);

    // fill with floor tiles by default
    FloorUtils.CarveGround(floor);

    FloorUtils.SurroundWithWalls(floor);
    FloorUtils.NaturalizeEdges(floor);

    // floor.PlaceUpstairs(new Vector2Int(1, floor.height / 2));
    floor.PlaceDownstairs(new Vector2Int(floor.width - 2, floor.height / 2));

    var soils = new List<Soil>();
    for (int x = 3; x < floor.width - 3; x += 3) {
      int y = floor.height / 2 - 2;
      if (floor.tiles[x, y] is Ground) {
        soils.Add(new Soil(new Vector2Int(x, y)));
      }
      y = floor.height / 2 + 2;
      if (floor.tiles[x, y] is Ground) {
        soils.Add(new Soil(new Vector2Int(x, y)));
      }
    }
    floor.PutAll(soils);

    var room0 = new Room(floor);
    floor.rooms = new List<Room> { room0 };
    floor.root = room0;

    EncounterGroup.Plants.GetRandomAndDiscount(1f)(floor, room0);

    Encounters.AddWater(floor, room0);
    Encounters.ThreeAstoriasInCorner(floor, room0);

    #if UNITY_EDITOR
    // Encounters.MatureStoutShrub(floor, room0);
    floor.depth = 20;
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

    floor.Put(new Altar(new Vector2Int(floor.width/2, floor.height - 2)));

    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }

  public Floor generateRewardFloor(int depth, params Encounter[] extraEncounters) {
    Floor floor = new Floor(depth, 16, 10);
    FloorUtils.CarveGround(floor);
    FloorUtils.SurroundWithWalls(floor);
    FloorUtils.NaturalizeEdges(floor);

    var room0 = new Room(floor);

    floor.PlaceUpstairs(new Vector2Int(room0.min.x, room0.max.y));
    floor.PlaceDownstairs(new Vector2Int(room0.max.x, room0.min.y));

    // Encounters.PlaceFancyGround(floor, room0);
    // Encounters.CavesRewards.GetRandomAndDiscount()(floor, room0);
    // EncounterGroup.Plants.GetRandomAndDiscount(0.9f)(floor, room0);
    // Encounters.AddTeleportStone(floor, room0);
    Encounters.AddOneWater(floor, room0);
    foreach (var encounter in extraEncounters) {
      encounter(floor, room0);
    }

    FloorUtils.TidyUpAroundStairs(floor);
    floor.root = room0;

    return floor;
  }

  /// <summary>
  /// Generates one single room with one wall variation, X mob encounters, Y grass encounters, an optional reward.
  /// Good for a contained experience.
  /// </summary>
  public Floor generateSingleRoomFloor(int depth, int width, int height, int numMobs = 1, int numGrasses = 1, bool reward = false, Encounter[] preMobEncounters = null, params Encounter[] extraEncounters) {
    Floor floor = tryGenerateSingleRoomFloor(depth, width, height, preMobEncounters == null);
    ensureConnectedness(floor);
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

    EncounterGroup.Spice.GetRandom()(floor, room0);
    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }

  private Floor tryGenerateSingleRoomFloor(int depth, int width, int height, bool defaultEncounters = true) {
    Floor floor = new Floor(depth, width, height);

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

    floor.PlaceUpstairs(new Vector2Int(room0.min.x, room0.max.y));
    floor.PlaceDownstairs(new Vector2Int(room0.max.x, room0.min.y));

    floor.root = room0;
    floor.rooms = new List<Room>();
    floor.upstairsRoom = room0;
    floor.downstairsRoom = room0;

    return floor;
  }

  public Floor generateBlobBossFloor(int depth) {
    Floor floor = new BossFloor(depth, 15, 15);
    // fill with wall
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Wall(p));
    }

    Room room0 = new Room(floor);
    FloorUtils.CarveGround(floor, floor.EnumerateCircle(room0.center, 7f));
    floor.Put(new Wall(room0.center + new Vector2Int(3, 3)));
    floor.Put(new Wall(room0.center + new Vector2Int(3, -3)));
    floor.Put(new Wall(room0.center + new Vector2Int(-3, -3)));
    floor.Put(new Wall(room0.center + new Vector2Int(-3, 3)));

    floor.PlaceUpstairs(new Vector2Int(1, floor.height / 2));
    floor.PlaceDownstairs(new Vector2Int(floor.width - 2, floor.height / 2));

    floor.root = room0;
    floor.rooms = new List<Room>();
    floor.upstairsRoom = room0;
    floor.downstairsRoom = room0;

    // add boss
    floor.Put(new Blobmother(room0.center + Vector2Int.right * 3));

    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }

  public Floor generateFungalColonyBossFloor(int depth) {
    Floor floor = new BossFloor(depth, 27, 13);
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Wall(p));
    }

    Room room0 = new Room(floor);

    void CutOutCircle(Vector2Int center, float radius, bool addBoss = false) {
      FloorUtils.CarveGround(floor, floor.EnumerateCircle(center, radius));
      // connect it back to center
      FloorUtils.CarveGround(floor, FloorUtils.Line3x3(floor, center, room0.center));
    }

    // start and end paths
    CutOutCircle(new Vector2Int(4, floor.height / 2), 2.5f);
    CutOutCircle(new Vector2Int(floor.width - 4, floor.height / 2), 2.5f);
    CutOutCircle(room0.center, 5.5f);
    floor.Put(new FungalColony(room0.center));

    // turn some of the walls into fungal walls
    foreach (var pos in floor.tiles.Where(t => t is Wall && floor.GetAdjacentTiles(t.pos).Any(t2 => t2.CanBeOccupied())).Select(t => t.pos).ToList()) {
      if (MyRandom.value < 0.25) {
        floor.Put(new FungalWall(pos));
      }
    }
    // block entrance
    floor.PutAll(new FungalWall(new Vector2Int(7, 5)), new FungalWall(new Vector2Int(7, 6)), new FungalWall(new Vector2Int(7, 7)));
    floor.PlaceUpstairs(new Vector2Int(2, floor.height / 2));
    floor.PlaceDownstairs(new Vector2Int(floor.width - 2, floor.height / 2));

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
    
    var mainland = makeGroup(floor.upstairs, ref walkableTiles);
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
      return floor.EnumerateLine(mainlandNode.pos, islandNode.pos)
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
