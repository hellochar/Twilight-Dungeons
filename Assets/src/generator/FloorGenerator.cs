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
  }

  public FloorGenerator(List<int> floorSeeds) {
    this.floorSeeds = floorSeeds;

    InitFloorGenerators();
  }

  private void InitFloorGenerators() {
    shared = new EncounterGroupShared();
    earlyGame = EncounterGroup.EarlyGame().AssignShared(shared);
    everything = EncounterGroup.EarlyMidMixed().AssignShared(shared);
    midGame = EncounterGroup.MidGame().AssignShared(shared);
    floorGenerators = new List<Func<Floor>>() {
      () => generateFloor0(0),
      () => generateSingleRoomFloor(1, 11, 11),
      () => generateSingleRoomFloor(2, 10, 10, extraEncounters: Encounters.OneAstoria),
      () => generateSingleRoomFloor(3, 9, 9, extraEncounters: Encounters.OneAstoria),
      () => generateSingleRoomFloor(4, 11, 11, 1, 1, true),
      () => generateSingleRoomFloor(5, 15, 11, 2),
      () => generateSingleRoomFloor(6, 13, 11, 2, extraEncounters: Encounters.OneAstoria),
      () => generateSingleRoomFloor(7, 11, 11, 2),
      () => generateRewardFloor(8, shared.Plants.GetRandomAndDiscount(0.9f), Encounters.OneAstoria),
      () => generateSingleRoomFloor(9, 13, 9, 2, 2),
      () => generateSingleRoomFloor(10, 14, 7, 2, 2),
      () => generateSingleRoomFloor(11, 20, 9, 3, 2),
      () => generateSingleRoomFloor(12, 20, 9, 4, 3, true),
      // () => generateBlobBossFloor(12),
      () => generateSingleRoomFloor(13, 12, 12, 4, 3),
      () => generateSingleRoomFloor(14, 15, 11, 4, 3),
      () => generateSingleRoomFloor(15, 20, 9, 5, 3),
      () => generateRewardFloor(16, shared.Plants.GetRandomAndDiscount(0.9f), Encounters.OneAstoria),
      () => generateMultiRoomFloor(17, 15, 15, 6),
      () => generateMultiRoomFloor(18, 30, 20, 7),
      () => generateMultiRoomFloor(19, 20, 20, 8, true),
      () => generateMultiRoomFloor(20, 24, 16, 9),
      () => generateMultiRoomFloor(21, 30, 12, 10),
      () => generateMultiRoomFloor(22, 30, 20, 15),
      () => generateMultiRoomFloor(23, 40, 20, 20),
      () => generateRewardFloor(24, shared.Plants.GetRandomAndDiscount(0.9f), Encounters.AddWater, Encounters.AddWater, Encounters.ThreeAstoriasInCorner),
      // () => generateSporeColonyBossFloor(24),
      () => generateSingleRoomFloor(25, 11, 11, 1, 2, true),
      () => generateMultiRoomFloor(26, 20, 13, 5, true),
      () => generateMultiRoomFloor(27, 30, 13, 7, true),
      () => generateMultiRoomFloor(28, 30, 12, 8, true),
      () => generateMultiRoomFloor(29, 24, 13, 9, true),
      () => generateMultiRoomFloor(30, 22, 12, 10, true),
      () => generateMultiRoomFloor(31, 45, 11, 15, true),
      () => generateEndFloor(32),
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
    Debug.Log("Depth " + depth + " seed " + floorSeeds[depth]);
    MyRandom.SetSeed(floorSeeds[depth]);

    // pick the generator
    var generator = floorGenerators[depth];
    Floor floor = null;

    int guard = 0;
    while (floor == null && guard++ < 20) {
      try {
        floor = generator();
      } catch (Exception e) {
        Debug.LogError(e);
        GameModel.main.turnManager.latestException = e;
      }
    }
    if (floor == null) {
      throw GameModel.main.turnManager.latestException;
    }
    if (floor.depth != depth) {
      throw new Exception("floorGenerator depth " + depth + " is marked as depth " + floor.depth);
    }

    #if UNITY_EDITOR
    // Encounters.AddNecroroot(floor, floor.root);
    // Encounters.AddPoisonmoss(floor, floor.root);
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
    Floor floor = new Floor(depth, 36, 36);

    // fill with floor tiles by default
    FloorUtils.PutGround(floor);
    FloorUtils.SurroundWithWalls(floor);
    FloorUtils.NaturalizeEdges(floor);

    var room0 = new Room(floor);
    floor.rooms = new List<Room> { room0 };
    floor.root = room0;

    // create concentric rings of walls
    foreach (var pos in floor.EnumerateFloor()) {
      var distance = Vector2.Distance(pos, room0.center);
      // distance 3.5 - 4 = -0.5
      // distance 4.5 - 4 = 
      var closestRing = Mathf.Round(distance / 6) * 6;
      var angle = Mathf.Atan2(pos.y - room0.center.y, pos.x - room0.center.x) * Mathf.Rad2Deg;
      var holeAngle = 360 * Util.Rand(closestRing);
      var angleDelta = Mathf.DeltaAngle(angle, holeAngle);
      if (Mathf.Abs(distance - closestRing) < 0.5f /*&& angleDelta < 30*/ && distance > 4) {
        floor.Put(new Wall(pos));
      }
    }

    floor.PlaceUpstairs(new Vector2Int(1, floor.height / 2));

    for (var i = 0; i < 12; i++) {
      Encounters.AddWater(floor, room0);
    }
    Encounters.FiftyRandomAstoria(floor, room0);
    for (var i = 0; i < 12; i++) {
      Encounters.AddGuardleaf(floor, room0);
      Encounters.AddGuardleaf(floor, room0);
      Encounters.AddGuardleaf(floor, room0);
      Encounters.AddGuardleaf(floor, room0);
    }

    foreach (var t in floor.EnumerateFloor().Where(p => floor.tiles[p] is Ground && floor.grasses[p] == null)) {
      floor.Put(new SoftGrass(t));
    }

    foreach (var pos in floor.EnumerateCircle(room0.center, 2.5f)) {
      var grass = floor.grasses[pos];
      if (grass != null) {
        floor.Remove(grass);
      }
      floor.Put(new FancyGround(pos));
    }

    /// add Ezra right in the middle
    floor.Put(new Ezra(room0.center));

    return floor;
  }

  public Floor generateFloor0(int depth) {
    Floor floor = new Floor(depth, 18, 14);

    // fill with floor tiles by default
    FloorUtils.PutGround(floor);

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

    void AddMaturePlant(System.Type t) {
      var loc = Util.RandomPick(soils);
      soils.Remove(loc);
      var constructor = t.GetConstructor(new System.Type[1] { typeof(Vector2Int) });
      var plant = (Plant) constructor.Invoke(new object[1] { loc.pos });
      plant.GoNextStage();
      plant.GoNextStage();
      floor.Put(plant);
    }

    var types = new List<System.Type> { typeof(BerryBush), typeof(Wildwood), typeof(Thornleaf), typeof(Weirdwood), typeof(Kingshroom), typeof(Frizzlefen), typeof(ChangErsWillow) };
    // AddMaturePlant(typeof(BerryBush));
    AddMaturePlant(Util.RandomPick(types));

    Encounters.AddWater(floor, room0);
    Encounters.ThreeAstoriasInCorner(floor, room0);

    #if UNITY_EDITOR
    floor.depth = 20;
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
    Floor floor = new Floor(depth, 16, 10);

    // fill with floor tiles by default
    FloorUtils.PutGround(floor);
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
  public Floor generateSingleRoomFloor(int depth, int width, int height, int numMobs = 1, int numGrasses = 1, bool reward = false, params Encounter[] extraEncounters) {
    Floor floor;
    do {
      floor = tryGenerateSingleRoomFloor(depth, width, height);
    } while (!AreStairsConnected(floor));
    var room0 = floor.root;
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
    #if UNITY_EDITOR
    // Encounters.AddParasite(floor, room0);
    #endif

    FloorUtils.TidyUpAroundStairs(floor);

    return floor;
  }

  private Floor tryGenerateSingleRoomFloor(int depth, int width, int height) {
    Floor floor = new Floor(depth, width, height);

    // fill with wall
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Wall(p));
    }

    Room room0 = new Room(floor);
    FloorUtils.PutGround(floor, floor.EnumerateRoom(room0));

    // one wall variation
    EncounterGroup.Walls.GetRandomAndDiscount()(floor, room0);

    FloorUtils.NaturalizeEdges(floor);
    floor.PlaceUpstairs(new Vector2Int(room0.min.x, room0.max.y));
    floor.PlaceDownstairs(new Vector2Int(room0.max.x, room0.min.y));

    floor.root = room0;
    floor.rooms = new List<Room>();
    floor.upstairsRoom = room0;
    floor.downstairsRoom = room0;

    return floor;
  }

  public Floor generateBlobBossFloor(int depth) {
    Floor floor = new Floor(depth, 14, 14);
    // fill with wall
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Wall(p));
    }

    Room room0 = new Room(floor);
    FloorUtils.PutGround(floor, floor.EnumerateCircle(room0.center, 7));
    FloorUtils.SurroundWithWalls(floor);
    FloorUtils.NaturalizeEdges(floor);

    Encounters.WallPillars(floor, room0);
    Encounters.WallPillars(floor, room0);

    floor.PlaceUpstairs(new Vector2Int(1, floor.height / 2));
    floor.PlaceDownstairs(new Vector2Int(floor.width - 2, floor.height / 2));

    floor.root = room0;
    floor.rooms = new List<Room>();
    floor.upstairsRoom = room0;
    floor.downstairsRoom = room0;

    // add boss
    floor.Put(new BlobBoss(room0.center));

    // fill remaining squares with soft grass
    foreach (var tile in floor.tiles.Where(tile => tile.grass == null && tile is Ground)) {
      floor.Put(new SoftGrass(tile.pos));
    }

    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }

  public Floor generateSporeColonyBossFloor(int depth) {
    Floor floor = new Floor(depth, 27, 27);
    // fill with wall
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Wall(p));
    }

    Room room0 = new Room(floor);

    void CutOutCircle(Vector2Int center, float radius) {
      FloorUtils.PutGround(floor, floor.EnumerateCircle(center, 6));
      // connect it back to center
      FloorUtils.PutGround(floor, FloorUtils.Line3x3(floor, center, room0.center));
    }
    CutOutCircle(room0.center, 6);
    var sideRoomInset = 7;
    var sideRoomRadius = 3f;
    CutOutCircle(new Vector2Int(sideRoomInset, sideRoomInset), sideRoomRadius);
    CutOutCircle(new Vector2Int(floor.width - 1 - sideRoomInset, sideRoomInset), sideRoomRadius);
    CutOutCircle(new Vector2Int(floor.width - 1 - sideRoomInset, floor.height - 1 - sideRoomInset), sideRoomRadius);
    CutOutCircle(new Vector2Int(sideRoomInset, floor.height - 1 - sideRoomInset), sideRoomRadius);

    // start and end paths
    CutOutCircle(new Vector2Int(4, floor.height / 2), 3);
    CutOutCircle(new Vector2Int(floor.width - 4, floor.height / 2), 3);

    FloorUtils.SurroundWithWalls(floor);
    FloorUtils.NaturalizeEdges(floor);

    floor.PlaceUpstairs(new Vector2Int(1, floor.height / 2));
    floor.PlaceDownstairs(new Vector2Int(floor.width - 2, floor.height / 2));

    floor.root = room0;
    floor.rooms = new List<Room>();
    floor.upstairsRoom = room0;
    floor.downstairsRoom = room0;

    // add boss
    floor.Put(new BlobBoss(room0.center));

    // fill remaining squares with soft grass
    foreach (var tile in floor.tiles.Where(tile => tile.grass == null && tile is Ground)) {
      floor.Put(new SoftGrass(tile.pos));
    }

    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }

  /// <summary>
  /// Generate a floor broken up into X smaller rooms, based on a number of "splits". Each room contains:
  /// one mob, one grass, one random encounter.
  /// </summary>
  public Floor generateMultiRoomFloor(int depth, int width = 60, int height = 20, int numSplits = 20, bool hasReward = false) {
    Floor floor;
    do {
      floor = tryGenerateMultiRoomFloor(depth, width, height, numSplits);
    } while (!AreStairsConnected(floor));

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
    var deadEndEncounters = EncounterGroup.Spice.Clone();
    foreach (var room in deadEndRooms) {
      if (MyRandom.value < 0.05f) {
        Encounters.SurroundWithRubble(floor, room);
      }
      var encounter = deadEndEncounters.GetRandomAndDiscount();
      encounter(floor, room);
    }

    foreach (var room in floor.rooms) {
      if (room != floor.upstairsRoom && room != rewardRoom) {
        // spawn a random encounter
        var encounter = EncounterGroup.Mobs.GetRandomAndDiscount();
        encounter(floor, room);
      }
    }

    floor.root.Traverse((room) => {
      // this includes abstract rooms!
      if (room.depth >= 2) {
        var encounter = EncounterGroup.Grasses.GetRandomAndDiscount();
        encounter(floor, room);
      }
    });

    FloorUtils.TidyUpAroundStairs(floor);

    return floor;
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
    List<Room> rooms = new List<Room>();
    root.Traverse(node => {
      if (node.isTerminal) {
        rooms.Add(node);
      }
    });

    // shrink it into a subset of the space available; adds more 'emptiness' to allow
    // for less rectangular shapes
    rooms.ForEach(room => room.randomlyShrink());

    foreach (var (a, b) in ComputeRoomConnections(rooms, root)) {
      FloorUtils.PutGround(floor, FloorUtils.Line3x3(floor, a, b));
    }

    rooms.ForEach(room => {
      // fill each room with floor
      FloorUtils.PutGround(floor, floor.EnumerateRoom(room));
    });

    FloorUtils.NaturalizeEdges(floor);

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
    root.Traverse(node => {
      if (!node.isTerminal) {
        Vector2Int nodeCenter = node.getCenter();
        RoomSplit split = node.split.Value;
        split.a.connections.Add(split.b);
        split.b.connections.Add(split.a);
        Vector2Int aCenter = split.a.getCenter();
        Vector2Int bCenter = split.b.getCenter();
        paths.Add((nodeCenter, aCenter));
        paths.Add((nodeCenter, bCenter));
      }
    });
    return paths;
  }

  public static bool AreStairsConnected(Floor floor) {
    var path = floor.FindPath(floor.downstairs.pos, floor.upstairs.pos);
    return path.Any();
  }
}
