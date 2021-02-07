using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FloorGenerator {
  public EncounterGroup EncounterGroup;

  public static Floor[] generateAll() {
    var generator = new FloorGenerator();
    return generator.generateAllFloors();
  }

  public Floor[] generateAllFloors() {
    var floors = new List<Floor>();
    EncounterGroup = EncounterGroup.EarlyGame();
    floors.Add(generateRestFloor(0));
    floors.Add(generateSingleRoomFloor(1, 9, 9));
    floors.Add(generateSingleRoomFloor(2, 10, 10));
    floors.Add(generateSingleRoomFloor(3, 11, 11));
    floors.Add(generateSingleRoomFloor(4, 11, 11, 1, 1, true));
    floors.Add(generateSingleRoomFloor(5, 20, 11, 2));
    floors.Add(generateSingleRoomFloor(6, 15, 11, 2));
    floors.Add(generateSingleRoomFloor(7, 11, 11, 2));
    floors.Add(generateRewardFloor(8, EncounterGroup.Plants.GetRandomAndDiscount(0.9f)));
    floors.Add(generateSingleRoomFloor(9, 13, 9, 2, 2));
    floors.Add(generateSingleRoomFloor(10, 14, 7, 2, 2));
    floors.Add(generateSingleRoomFloor(11, 20, 9, 3, 2));
    EncounterGroup = EncounterGroup.EarlyMidMixed();
    floors.Add(generateSingleRoomFloor(12, 10, 10, 2, 2, true)); // make this a miniboss level
    floors.Add(generateSingleRoomFloor(13, 12, 12, 3, 2));
    floors.Add(generateSingleRoomFloor(14, 15, 11, 3, 3));
    floors.Add(generateSingleRoomFloor(15, 20, 9, 4, 3));
    floors.Add(generateRewardFloor(16, EncounterGroup.Plants.GetRandomAndDiscount(0.9f)));
    floors.Add(generateMultiRoomFloor(17, 15, 15, 6));
    floors.Add(generateMultiRoomFloor(18, 20, 20, 6));
    floors.Add(generateMultiRoomFloor(19, 30, 20, 7));
    floors.Add(generateMultiRoomFloor(20, 20, 20, 8, true));
    floors.Add(generateMultiRoomFloor(21, 24, 16, 9));
    floors.Add(generateMultiRoomFloor(22, 30, 12, 10));
    floors.Add(generateMultiRoomFloor(23, 30, 20, 15));
    floors.Add(generateRewardFloor(24, Encounters.Twice(Encounters.AddWater), Encounters.ThreeAstoriasInCorner));
    EncounterGroup = EncounterGroup.MidGame();
    floors.Add(generateSingleRoomFloor(25, 11, 11, 1, 2, true));
    floors.Add(generateMultiRoomFloor(26, 20, 13, 5, true));
    floors.Add(generateMultiRoomFloor(27, 30, 13, 7, true));
    floors.Add(generateMultiRoomFloor(28, 30, 12, 8, true));
    floors.Add(generateMultiRoomFloor(29, 24, 13, 9, true));
    floors.Add(generateMultiRoomFloor(30, 22, 12, 10, true));
    floors.Add(generateMultiRoomFloor(31, 45, 11, 15, true));
    floors.Add(generateRewardFloor(32, EncounterGroup.Plants.GetRandomAndDiscount(0.9f)));
    // floors.Add(generateBossFloor(33));
    // floors.Add(generateEndFloor(34));
    return floors.ToArray();
  }
  
  public Floor generateRestFloor(int depth) {
    Floor floor = new Floor(depth, 22, 14);

    // fill with floor tiles by default
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Ground(p));
    }

    FloorUtils.SurroundWithWalls(floor);
    FloorUtils.NaturalizeEdges(floor);

    // floor.PlaceUpstairs(new Vector2Int(1, floor.height / 2));
    floor.PlaceDownstairs(new Vector2Int(floor.width - 2, floor.height / 2));

    var soils = new List<Soil>();
    for (int x = 2; x < floor.width - 2; x += 2) {
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

    var types = new List<System.Type> { typeof(BerryBush), typeof(Wildwood), typeof(Thornleaf), typeof(Weirdwood), typeof(Kingshroom) };
    // AddMaturePlant(typeof(Weirdwood));
    // AddMaturePlant(typeof(Weirdwood));
    AddMaturePlant(Util.RandomPick(types));
    AddMaturePlant(Util.RandomPick(types));

    Encounters.AddWater(floor, room0);
    Encounters.ThreeAstoriasInCorner(floor, room0);

    #if UNITY_EDITOR
    floor.depth = 20;
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

    return floor;
  }

  public Floor generateRewardFloor(int depth, params Encounter[] extraEncounters) {
    Floor floor = new Floor(depth, 16, 10);

    // fill with floor tiles by default
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Ground(p));
    }
    FloorUtils.SurroundWithWalls(floor);
    FloorUtils.NaturalizeEdges(floor);

    var room0 = new Room(floor);

    floor.PlaceUpstairs(new Vector2Int(room0.min.x, room0.max.y), false);
    floor.PlaceDownstairs(new Vector2Int(room0.max.x, room0.min.y), false);

    Encounters.PlaceFancyGround(floor, room0);
    // Encounters.CavesRewards.GetRandomAndDiscount()(floor, room0);
    // EncounterGroup.Plants.GetRandomAndDiscount(0.9f)(floor, room0);
    // Encounters.AddTeleportStone(floor, room0);
    foreach (var encounter in extraEncounters) {
      encounter(floor, room0);
    }

    // just do nothing on this floor
    return floor;
  }

  /// <summary>
  /// Generates one single room with one wall variation, X mob encounters, Y grass encounters, an optional reward.
  /// Good for a contained experience.
  /// </summary>
  public Floor generateSingleRoomFloor(int depth, int width, int height, int numMobs = 1, int numGrasses = 1, bool reward = false) {
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

    // a reward (optional)
    if (reward) {
      EncounterGroup.Rewards.GetRandomAndDiscount()(floor, room0);
      Encounters.AddWater(floor, room0);
    }

    EncounterGroup.Spice.GetRandom()(floor, room0);
    #if UNITY_EDITOR
    // Encounters.AddParasite(floor, room0);
    #endif

    if (floor.tiles[floor.upstairs.landing] is Wall) {
      floor.Put(new Ground(floor.upstairs.landing));
    }
    if (floor.tiles[floor.downstairs.landing] is Wall) {
      floor.Put(new Ground(floor.downstairs.landing));
    }

    // clear stairs so player doesn't walk right into bad grasses or get immediately surrounded by enemies
    foreach (var tile in floor.GetAdjacentTiles(floor.upstairs.pos)) {
      if (tile.grass != null) {
        floor.Remove(tile.grass);
      }
      if (tile.actor != null) {
        var newSpot = Util.RandomPick(floor.EnumerateRoomTiles(room0).Where((x) => x.CanBeOccupied()));
        // move the actor to a different spot in the map
        tile.actor.pos = newSpot.pos;
      }
    }

    return floor;
  }

  private Floor tryGenerateSingleRoomFloor(int depth, int width, int height) {
    Floor floor = new Floor(depth, width, height);

    // fill with wall
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Wall(p));
    }

    Room room0 = new Room(floor);
    foreach (var pos in floor.EnumerateRoom(room0)) {
      floor.Put(new Ground(pos));
    }

    // one wall variation
    EncounterGroup.Walls.GetRandomAndDiscount()(floor, room0);

    FloorUtils.NaturalizeEdges(floor);
    floor.PlaceUpstairs(new Vector2Int(room0.min.x, room0.max.y), false);
    floor.PlaceDownstairs(new Vector2Int(room0.max.x, room0.min.y), false);

    floor.root = room0;
    floor.rooms = new List<Room>();
    floor.upstairsRoom = room0;
    floor.downstairsRoom = room0;

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
      if (Random.value < 0.05f) {
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
      /// create a 3x3 tunnel
      var tunnelPath3x3 = floor
        .EnumerateLine(a, b)
        .SelectMany((pos) => floor.GetAdjacentTiles(pos).Select(t => t.pos));
      foreach (var point in tunnelPath3x3) {
        floor.Put(new Ground(point));
      }
    }

    rooms.ForEach(room => {
      // fill each room with floor
      foreach (var pos in floor.EnumerateRoom(room)) {
        floor.Put(new Ground(pos));
      }
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
