using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class FloorGenerator {
  public static Floor generateRestFloor(int depth) {
    Floor floor = new Floor(depth, 22, 14);

    // fill with floor tiles by default
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Ground(p));
    }

    // surround floor with walls
    for (int x = 0; x < floor.width; x++) {
      floor.Put(new Wall(new Vector2Int(x, 0)));
      floor.Put(new Wall(new Vector2Int(x, floor.height - 1)));
    }
    for (int y = 0; y < floor.height; y++) {
      floor.Put(new Wall(new Vector2Int(0, y)));
      floor.Put(new Wall(new Vector2Int(floor.width - 1, y)));
    }

    SMOOTH_ROOM_EDGES.ApplyWithRotations(floor);
    SMOOTH_WALL_EDGES.ApplyWithRotations(floor);
    MAKE_WALL_BUMPS.ApplyWithRotations(floor);

    floor.PlaceUpstairs(new Vector2Int(1, floor.height / 2));
    floor.PlaceDownstairs(new Vector2Int(floor.width - 2, floor.height / 2));

    var soils = new List<Soil>();
    for (int x = 4; x < floor.width - 4; x += 4) {
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

    var loc1 = Util.RandomPick(soils);
    soils.Remove(loc1);
    var berryBush = new BerryBush(loc1.pos);
    berryBush.GoNextStage();
    berryBush.GoNextStage();
    floor.Put(berryBush);

    var loc2 = Util.RandomPick(soils);
    soils.Remove(loc2);
    var wildWood = new Wildwood(loc2.pos);
    wildWood.GoNextStage();
    wildWood.GoNextStage();
    floor.Put(wildWood);

    Encounters.ThreePlumpAstoriasInCorner(floor, room0);
    // Encounters.AddBrambles(floor, room0);
    // Encounters.ScatteredBoombugs(floor, room0);
    // Encounters.AddWater(floor, room0);
    // Encounters.BatInCorner(floor, room0);
    // Encounters.ScatteredBoombugs.Apply(floor, room0);
    // Encounters.AFewSnails(floor, room0);
    // Encounters.AFewBlobs(floor, room0);

    return floor;
  }

  public static bool AreStairsConnected(Floor floor) {
    var path = floor.FindPath(floor.downstairs.pos, floor.upstairs.pos);
    return path.Any();
  }

  public static Floor generateRandomFloor(int depth) {
    Floor floor;
    do {
      floor = tryGenerateRandomFloor(depth);
    } while (!AreStairsConnected(floor));

    // floor.ComputeConnectivity();

    var intermediateRooms = floor.rooms
      .Where((room) => room != floor.upstairsRoom && room != floor.downstairsRoom);
    
    // the non-downstairs terminal room farthest away from the upstairs according to pathfinding
    var rewardRoom = intermediateRooms
      .OrderByDescending((room) => floor.FindPath(floor.upstairs.pos, room.center).Count)
      .First();

    Encounters.PlaceFancyGround(floor, rewardRoom);
    Encounters.SurroundWithRubble(floor, rewardRoom);
    var rewardEncounter = Encounters.CavesRewards.GetRandom();
    rewardEncounter(floor, rewardRoom);

    var deadEndRooms = intermediateRooms.Where((room) => room != rewardRoom && room.connections.Count < 2);
    foreach (var room in deadEndRooms) {
      if (Random.value < 0.2f) {
        Encounters.SurroundWithRubble(floor, room);
      }
      var encounter = Encounters.CavesDeadEnds.GetRandom();
      encounter(floor, room);
    }

    // Add mobs; each time a mob encounter is added, the chance it happens again
    // is discounted by this much.
    var discount = 0.25f;
    var mobs = Encounters.CavesMobs.Clone();
    foreach (var room in floor.rooms) {
      if (room != floor.upstairsRoom && room != rewardRoom) {
        // spawn a random encounter
        var encounter = mobs.GetRandomAndDiscount(discount);
        encounter(floor, room);
      }
    }

    var grasses = Encounters.CavesGrasses.Clone();
    floor.root.Traverse((room) => {
      // this includes abstract rooms!
      if (room.depth >= 2) {
        var encounter = grasses.GetRandomAndDiscount(discount);
        encounter(floor, room);
      }
    });

    return floor;
  }

  /// connectivity is not guaranteed
  private static Floor tryGenerateRandomFloor(int depth) {
    Floor floor = new Floor(depth, 60, 20);

    // fill with wall
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Wall(p));
    }

    // randomly partition space into 20 rooms
    Room root = new Room(floor);
    for (int i = 0; i < 20; i++) {
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
      foreach (var point in floor.EnumerateLine(a, b)) {
        floor.Put(new Ground(point));
      }
    }

    rooms.ForEach(room => {
      // fill each room with floor
      foreach (var pos in floor.EnumerateRoom(room)) {
        floor.Put(new Ground(pos));
      }
    });

    // apply a natural look across the floor by smoothing both wall corners and space corners
    SMOOTH_ROOM_EDGES.ApplyWithRotations(floor);
    SMOOTH_WALL_EDGES.ApplyWithRotations(floor);
    MAKE_WALL_BUMPS.ApplyWithRotations(floor);

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

  static ShapeTransform SMOOTH_WALL_EDGES = new ShapeTransform(
    new int[3, 3] {
      {1, 1, 1},
      {0, 0, 1},
      {0, 0, 1},
    },
    1
  );

  static ShapeTransform SMOOTH_ROOM_EDGES = new ShapeTransform(
    new int[3, 3] {
      {0, 0, 0},
      {1, 1, 0},
      {1, 1, 0},
    },
    0
  );

  static ShapeTransform MAKE_WALL_BUMPS = new ShapeTransform(
    new int[3, 3] {
      {0, 0, 0},
      {1, 1, 1},
      {1, 1, 1},
    },
    0,
    // 50% chance to make a 2-run
    1 - Mathf.Sqrt(0.5f)
  // 0.33f
  );
}
