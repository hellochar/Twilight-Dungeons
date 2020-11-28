using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class FloorGenerator {
  public static Floor generateFloor0() {
    Floor floor = new Floor(22, 14);

    // fill with floor tiles by default
    floor.ForEachLocation(p => floor.tiles.Put(new Ground(p)));

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

  public static Floor generateRandomFloor() {
    Floor floor;
    do {
      floor = tryGenerateRandomFloor();
    } while (!floor.AreStairsConnected());

    // foreach (var room in floor.rooms) {
    //   // spawn a random encounter
    //   var encounterType = Random.Range(0, 5);
    //   switch (encounterType) {
    //     case 0:

    //     break;
    //     case 1:
    //     break;
    //     case 2:
    //     break;
    //   }
    // }

    floor.AddActor(new Jackal(new Vector2Int(floor.width / 2 + 1, floor.height / 2 + 1)));
    floor.AddActor(new Jackal(new Vector2Int(floor.width / 2 + 1, floor.height / 2 - 1)));
    floor.AddActor(new Jackal(new Vector2Int(floor.width / 2 + 2, floor.height / 2)));
    floor.AddActor(new Jackal(new Vector2Int(floor.width / 2 + 2, floor.height / 2 + 1)));

    floor.AddActor(new Blob(new Vector2Int(floor.width / 2, floor.height / 2)));
    floor.AddActor(new Blob(new Vector2Int(floor.width / 3, floor.height / 3)));
    floor.AddActor(new Blob(new Vector2Int(floor.width / 3, floor.height / 3)));
    floor.AddActor(new Blob(new Vector2Int(floor.width / 3, floor.height / 3)));
    floor.AddActor(new Blob(new Vector2Int(floor.width / 3, floor.height / 3)));
    return floor;
  }

  /// connectivity is not guaranteed
  private static Floor tryGenerateRandomFloor() {
    Floor floor = new Floor(60, 20);

    // fill with wall
    floor.ForEachLocation(p => floor.tiles.Put(new Wall(p)));

    // randomly partition space into 20 rooms
    BSPNode root = new BSPNode(null, new Vector2Int(1, 1), new Vector2Int(floor.width - 2, floor.height - 2));
    for (int i = 0; i < 20; i++) {
      bool success = root.randomlySplit();
      if (!success) {
        Debug.Log("couldn't split at iteration " + i);
        break;
      }
    }

    // collect all rooms
    List<BSPNode> rooms = new List<BSPNode>();
    root.Traverse(node => {
      if (node.isTerminal) {
        rooms.Add(node);
      }
    });

    // shrink it into a subset of the space available; adds more 'emptiness' to allow
    // for tunnels
    rooms.ForEach(room => room.randomlyShrink());

    foreach (var (a, b) in ConnectRooms(rooms, root)) {
      floor.ForEachLine(a, b, point => floor.tiles.Put(new Ground(point)));
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
    BSPNode upstairsRoom = rooms.First();
    // 1-px padding from the top-left of the room
    Vector2Int upstairsPos = new Vector2Int(upstairsRoom.min.x + 1, upstairsRoom.max.y - 1);
    floor.PlaceUpstairs(upstairsPos);

    BSPNode downstairsRoom = rooms.Last();
    // 1-px padding from the bottom-right of the room
    Vector2Int downstairsPos = new Vector2Int(downstairsRoom.max.x - 1, downstairsRoom.min.y + 1);
    floor.PlaceDownstairs(downstairsPos);
    floor.rooms = rooms;
    return floor;
  }

  /// Connect all the rooms together with at least one through-path
  static List<(Vector2Int, Vector2Int)> ConnectRooms(List<BSPNode> rooms, BSPNode root) {
    return ConnectRoomsBSPSiblings(rooms, root);
  }

  /// draw a path connecting siblings together, including intermediary nodes (guarantees connectedness)
  /// this tends to draw long lines that cut right through single thickness walls
  static List<(Vector2Int, Vector2Int)> ConnectRoomsBSPSiblings(List<BSPNode> rooms, BSPNode root) {
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
