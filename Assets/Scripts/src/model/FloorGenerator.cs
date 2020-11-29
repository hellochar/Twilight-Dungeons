using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class FloorGenerator {
  public static Floor generateFloor0() {
    Floor floor = new Floor(22, 14);

    // fill with floor tiles by default
    foreach (var p in floor.EnumerateFloor()) {
      floor.tiles.Put(new Ground(p));
    }

    // surround floor with walls
    for (int x = 0; x < floor.width; x++) {
      floor.tiles.Put(new Wall(new Vector2Int(x, 0)));
      floor.tiles.Put(new Wall(new Vector2Int(x, floor.height - 1)));
    }
    for (int y = 0; y < floor.height; y++) {
      floor.tiles.Put(new Wall(new Vector2Int(0, y)));
      floor.tiles.Put(new Wall(new Vector2Int(floor.width - 1, y)));
    }

    SMOOTH_ROOM_EDGES.ApplyWithRotations(floor);
    SMOOTH_WALL_EDGES.ApplyWithRotations(floor);
    MAKE_WALL_BUMPS.ApplyWithRotations(floor);

    floor.PlaceUpstairs(new Vector2Int(1, floor.height / 2));
    floor.PlaceDownstairs(new Vector2Int(floor.width - 2, floor.height / 2));

    for (int x = 4; x < floor.width - 4; x += 4) {
      int y = floor.height / 2 - 2;
      if (floor.tiles[x, y] is Ground) {
        floor.tiles.Put(new Soil(new Vector2Int(x, y)));
      }
      y = floor.height / 2 + 2;
      if (floor.tiles[x, y] is Ground) {
        floor.tiles.Put(new Soil(new Vector2Int(x, y)));
      }
    }

    return floor;
  }

  class Encounter {
    public Encounter(System.Action<Floor, Room> apply) {
      this.Apply = apply;
    }

    public System.Action<Floor, Room> Apply { get; }
  }

  // no op
  static Encounter Encounter0 = new Encounter((Floor, Room) => {});

  static Encounter Encounter1 = new Encounter((floor, room) => {
    var emptyTilesInRoom = floor.EnumerateRoomTiles(room).Where(t => t.CanBeOccupied()).ToList();
    emptyTilesInRoom.Sort((x, y) => Random.value < 0.5 ? -1 : 1);
    foreach (var tile in emptyTilesInRoom.Take(3)) {
      floor.AddActor(new Blob(tile.pos));
    }
  });

  static Encounter Encounter2 = new Encounter((floor, room) => {
    var emptyTilesInRoom = floor.EnumerateRoomTiles(room).Where(t => t.CanBeOccupied()).ToList();
    emptyTilesInRoom.Sort((x, y) => Random.value < 0.5 ? -1 : 1);
    emptyTilesInRoom.Sort((x, y) => Vector2Int.Distance(x.pos, room.center) < Vector2Int.Distance(y.pos, room.center) ? -1 : 1);
    var numJackals = Random.Range(4, 7);
    foreach (var tile in emptyTilesInRoom.Take(numJackals)) {
      floor.AddActor(new Jackal(tile.pos));
    }
  });

  /// bats line the edges
  static Encounter Encounter3 = new Encounter((floor, room) => {
    var emptyTilesInRoom = floor.EnumerateRoomTiles(room).Where(t => t.CanBeOccupied()).ToList();
    // sort by farthest distance to center to nearest
    emptyTilesInRoom.Sort((x, y) => Vector2Int.Distance(x.pos, room.center) < Vector2Int.Distance(y.pos, room.center) ? 1 : -1);
    foreach (var tile in emptyTilesInRoom.Take(4)) {
      floor.AddActor(new Bat(tile.pos));
    }
  });

  static Encounter Encounter4 = new Encounter((floor, room) => {
    // add a soil at the center
    var emptyTilesInRoom = floor.EnumerateRoomTiles(room).Where(t => t.CanBeOccupied()).ToList();
    emptyTilesInRoom.Sort((x, y) => Vector2Int.Distance(x.pos, room.center) < Vector2Int.Distance(y.pos, room.center) ? -1 : 1);
    var emptyTileNearestCenter = emptyTilesInRoom.FirstOrDefault();

    if (emptyTileNearestCenter != null && !(emptyTileNearestCenter is Downstairs || emptyTileNearestCenter is Upstairs)) {
      floor.tiles.Put(new Soil(emptyTileNearestCenter.pos));
      var bush = new BerryBush(emptyTileNearestCenter.pos);
      // jump to Mature
      bush.stage = bush.stage.NextStage.NextStage;
      floor.AddActor(bush);
    }
  });

  static WeightedRandomBag<Encounter> Encounters = new WeightedRandomBag<Encounter> {
    { 2, Encounter0 },
    { 1, Encounter1 },
    { 1, Encounter2 },
    { 1, Encounter3 },
    { 1, Encounter4 },
  };

  public static Floor generateRandomFloor() {
    Floor floor;
    do {
      floor = tryGenerateRandomFloor();
    } while (!floor.AreStairsConnected());

    foreach (var room in floor.rooms) {
      // spawn a random encounter
      var encounter = Encounters.GetRandom();
      encounter.Apply(floor, room);
    }

    return floor;
  }

  /// connectivity is not guaranteed
  private static Floor tryGenerateRandomFloor() {
    Floor floor = new Floor(60, 20);

    // fill with wall
    foreach (var p in floor.EnumerateFloor()) {
      floor.tiles.Put(new Wall(p));
    }

    // randomly partition space into 20 rooms
    Room root = new Room(null, new Vector2Int(1, 1), new Vector2Int(floor.width - 2, floor.height - 2));
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
        floor.tiles.Put(new Ground(point));
      }
    }

    rooms.ForEach(room => {
      // fill each room with floor
      for (int x = room.min.x; x <= room.max.x; x++) {
        for (int y = room.min.y; y <= room.max.y; y++) {
          floor.tiles.Put(new Ground(new Vector2Int(x, y)));
        }
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
        Vector2Int aCenter = node.split.Value.a.getCenter();
        Vector2Int bCenter = node.split.Value.b.getCenter();
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
