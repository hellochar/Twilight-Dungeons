using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class FloorGenerator {
  public static Floor generateRestFloor(int depth) {
    Floor floor = new Floor(depth, 14, 10);

    // fill with floor tiles by default
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Ground(p));
    }

    FloorUtils.SurroundWithWalls(floor);

    floor.PlaceUpstairs(new Vector2Int(1, floor.height / 2));
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

    var types = new List<System.Type> { typeof(BerryBush), typeof(Wildwood), typeof(Thornleaf) };
    AddMaturePlant(Util.RandomPick(types));

    Encounters.ThreePlumpAstoriasInCorner(floor, room0);
    // Encounters.OneButterfly(floor, room0);
    // Encounters.OneSpider(floor, room0);
    // Encounters.AddSpore(floor, room0);
    // Encounters.AddBladegrass(floor, room0);
    // Encounters.ScatteredBoombugs(floor, room0);
    Encounters.AddWater(floor, room0);
    // Encounters.BatInCorner(floor, room0);
    // Encounters.ScatteredBoombugs.Apply(floor, room0);
    // Encounters.AFewSnails(floor, room0);
    // Encounters.AFewBlobs(floor, room0);

    return floor;
  }

  public static Floor generateRewardFloor(int depth) {
    Floor floor = new Floor(depth, 8, 8);

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
    Encounters.CavesRewards.GetRandomAndDiscount()(floor, room0);
    Encounters.CavesPlantRewards.GetRandomAndDiscount(0.9f)(floor, room0);

    // just do nothing on this floor
    return floor;
  }

  public static bool AreStairsConnected(Floor floor) {
    var path = floor.FindPath(floor.downstairs.pos, floor.upstairs.pos);
    return path.Any();
  }

  public static Floor generateTinyFloor(int depth, int width, int height, int numMobs = 1, int numGrasses = 1, bool reward = false) {
    Floor floor = new Floor(depth, width, height);

    // fill with wall
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Wall(p));
    }

    Room room0 = new Room(floor);
    foreach (var pos in floor.EnumerateRoom(room0)) {
      floor.Put(new Ground(pos));
    }

    FloorUtils.NaturalizeEdges(floor);
    floor.PlaceUpstairs(new Vector2Int(room0.min.x, room0.max.y), false);
    floor.PlaceDownstairs(new Vector2Int(room0.max.x, room0.min.y), false);

    floor.root = room0;
    floor.rooms = new List<Room>();
    floor.upstairsRoom = room0;
    floor.downstairsRoom = room0;

    Encounters.CavesWalls.GetRandomAndDiscount()(floor, room0);
    for (var i = 0; i < numMobs; i++) {
      Encounters.CavesMobs.GetRandomAndDiscount()(floor, room0);
    }
    for (var i = 0; i < numGrasses; i++) {
      Encounters.CavesGrasses.GetRandomAndDiscount()(floor, room0);
    }
    if (reward) {
      Encounters.CavesRewards.GetRandomAndDiscount()(floor, room0);
    }
    Encounters.CavesDeadEnds.GetRandomAndDiscount()(floor, room0);

    // clear stairs so player doesn't walk into a shitshow
    foreach (var tile in floor.GetAdjacentTiles(floor.upstairs.pos)) {
      tile.grass?.Kill();
      if (tile.actor != null) {
        var newSpot = Util.RandomPick(floor.EnumerateRoomTiles(room0).Where((x) => x.CanBeOccupied()));
        // move the actor to a different spot in the map
        tile.actor.pos = newSpot.pos;
      }
    }

    return floor;
  }

  public static Floor generateRandomFloor(int depth, int width = 60, int height = 20, int numSplits = 20) {
    Floor floor;
    do {
      floor = tryGenerateRandomFloor(depth, width, height, numSplits);
    } while (!AreStairsConnected(floor));

    // floor.ComputeConnectivity();

    var intermediateRooms = floor.rooms
      .Where((room) => room != floor.upstairsRoom && room != floor.downstairsRoom);
    
    // the non-downstairs terminal room farthest away from the upstairs according to pathfinding
    var rewardRoom = intermediateRooms
      .OrderByDescending((room) => floor.FindPath(floor.upstairs.pos, room.center).Count)
      .First();

    Encounters.PlaceFancyGround(floor, rewardRoom);
    // Encounters.SurroundWithRubble(floor, rewardRoom);
    var rewardEncounter = Encounters.CavesRewards.GetRandomAndDiscount();
    rewardEncounter(floor, rewardRoom);

    var deadEndRooms = intermediateRooms.Where((room) => room != rewardRoom && room.connections.Count < 2);
    foreach (var room in deadEndRooms) {
      if (Random.value < 0.05f) {
        Encounters.SurroundWithRubble(floor, room);
      }
      var encounter = Encounters.CavesDeadEnds.GetRandom();
      encounter(floor, room);
    }

    // Add mobs; each time a mob encounter is added, the chance it happens again
    // is discounted by this much.
    foreach (var room in floor.rooms) {
      if (room != floor.upstairsRoom && room != rewardRoom) {
        // spawn a random encounter
        var encounter = Encounters.CavesMobs.GetRandomAndDiscount();
        encounter(floor, room);
      }
    }

    floor.root.Traverse((room) => {
      // this includes abstract rooms!
      if (room.depth >= 2) {
        var encounter = Encounters.CavesGrasses.GetRandomAndDiscount();
        encounter(floor, room);
      }
    });

    return floor;
  }

  /// connectivity is not guaranteed
  private static Floor tryGenerateRandomFloor(int depth, int width, int height, int numSplits) {
    Floor floor = new Floor(depth, width, height);

    // fill with wall
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Wall(p));
    }

    // randomly partition space into  rooms
    Room root = new Room(floor);
    for (int i = 0; i < numSplits; i++) {
      bool success = root.randomlySplit();
      if (!success) {
        Debug.Log("couldn't split at iteration " + i);
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
    // for tunnels
    rooms.ForEach(room => room.randomlyShrink());

    foreach (var (a, b) in ConnectRooms(rooms, root)) {
      foreach (var point in floor.EnumerateLine(a, b).SelectMany((pos) => floor.GetAdjacentTiles(pos).Select(t => t.pos))) {
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

    // sort by distance to top-left.
    Vector2Int topLeft = new Vector2Int(0, floor.height);
    rooms.Sort((a, b) => {
      int aDist2 = Util.manhattanDistance(a.getTopLeft() - topLeft);
      int bDist2 = Util.manhattanDistance(b.getTopLeft() - topLeft);

      if (aDist2 < bDist2) {
        return -1;
      } else if (aDist2 > bDist2) {
        return 1;
      }
      return 0;
    });
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
  static List<(Vector2Int, Vector2Int)> ConnectRooms(List<Room> rooms, Room root) {
    return ConnectRoomsBSPSiblings(rooms, root);
  }

  /// draw a path connecting siblings together, including intermediary nodes (guarantees connectedness)
  /// this tends to draw long lines that cut right through single thickness walls
  static List<(Vector2Int, Vector2Int)> ConnectRoomsBSPSiblings(List<Room> rooms, Room root) {
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
}
