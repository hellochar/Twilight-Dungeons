using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class MultiRoomHomeFloor : HomeFloor {
  public static MultiRoomHomeFloor generate() {
    var floor = new MultiRoomHomeFloor(40, 40);
    int numSplits = 30;

    // fill with wall
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Wall(p));
    }

    // randomly partition space into rooms
    Room root = new Room(floor);
    for (int i = 0; i < numSplits; i++) {
      // bool success = root.randomlySplit();
      var biggestRoom = root.Traverse().Where(r => r.isTerminal).OrderByDescending(room => room.width * room.height).FirstOrDefault();
      bool success = biggestRoom != null ? biggestRoom.randomlySplit() : false;
      if (!success) {
        Debug.LogWarning("couldn't split at iteration " + i);
        break;
      }
    }

    Vector2Int floorCenter = floor.center;

    // collect all rooms and sort by distance to center
    List<Room> rooms = root.Traverse()
      .Where(n => n.isTerminal)
      // shrink it into a subset of the space available; adds more 'emptiness' to allow
      // for less rectangular shapes
      .Select(room => { room.randomlyShrink(); return room; })
      // .Select(room => {
      //   if (room.width >= 5 && room.height >= 5) {
      //     room.Shrink(1);
      //   }
      //   return room;
      // })
      .OrderBy(room => Util.manhattanDistance(room.min))
      .ToList();

    Room startingRoom = rooms.First();
    floor.SetStartPos(startingRoom.center);

    floor.root = root;
    floor.rooms = rooms;
    // floor.startRoom = upstairsRoom;
    // floor.downstairsRoom = downstairsRoom;

    // fill each room with floor
    rooms.ForEach(room => {
      floor.PutAll(floor.EnumerateRoom(room).Select(pos => new Ground(pos)));
    });

    // connect rooms using the connectedness algorithm. creates really dense and tight rooms.
    // var useConnectednessAlgorithm = false;
    // if (useConnectednessAlgorithm) {
    //   FloorGenerator.ensureConnectedness(floor);
    // } else {
    //   // add connections between bsp siblings
      // foreach (var (a, b) in FloorGenerator.ComputeRoomConnections(rooms, root)) {
      //   FloorUtils.CarveGround(floor, FloorUtils.Line3x3(floor, a, b));
      //   // FloorUtils.CarveGround(floor, floor.EnumerateLine(a, b));
      // }
    // }

    // finds the endpoints of a straight line connecting room1 and room2 if one exists.
    // assumes rooms don't overlap
    (Vector2Int, Vector2Int)? FindStraightLineConnection(Room room1, Room room2) {
      // ranges are inclusive on both ends!
      List<int> GetOverlaps(int firstMin, int firstMax, int secondMin, int secondMax) {
        var overlappedMin = Mathf.Max(firstMin, secondMin);
        var overlappedMax = Mathf.Min(firstMax, secondMax);
        var list = new List<int>();
        for(var i = overlappedMin; i <= overlappedMax; i++) {
          list.Add(i);
        }
        return list;
      }


      // case 1 - rooms can connect horizontally 
      var horizontalOverlaps = GetOverlaps(room1.min.y, room1.max.y, room2.min.y, room2.max.y);
      if (horizontalOverlaps.Any()) {
        // if we have horizontal overlaps, this implies we have no vertical overlaps.
        // this means we can connect the max X of one room to the min X of the other room
        var chosenY = Util.RandomPick(horizontalOverlaps);
        var lowerMaxX = Mathf.Min(room1.max.x, room2.max.x);
        var higherMinX = Mathf.Max(room1.min.x, room2.min.x);
        return (new Vector2Int(lowerMaxX, chosenY), new Vector2Int(higherMinX, chosenY));
      }

      // case 2 - rooms can connect vertically
      var verticalOverlaps = GetOverlaps(room1.min.x, room1.max.x, room2.min.x, room2.max.x);
      if (verticalOverlaps.Any()) {
        var chosenX = Util.RandomPick(verticalOverlaps);
        var lowerMaxY = Mathf.Min(room1.max.y, room2.max.y);
        var higherMinY = Mathf.Max(room1.min.y, room2.min.y);
        return (new Vector2Int(chosenX, lowerMaxY), new Vector2Int(chosenX, higherMinY));
      }

      return null;
    }

    // Kruskal's algorithm:

    // convert terminal rooms into a graph where rooms are vertices 
    // and connect all rooms together by their straight line connection (if they have one)
    List<Edge> allEdges = new List<Edge>();
    for (int i = 0; i < rooms.Count; i++) {
      var room1 = rooms[i];
      room1.name = "r" + i;
      for (int j = i + 1; j < rooms.Count; j++) {
        var room2 = rooms[j];
        var connection = FindStraightLineConnection(room1, room2);
        if (connection.HasValue) {
          allEdges.Add(new Edge{ room1 = room1, room2 = room2, connection = connection.Value });
        }
      }
    }
    // prevent internal ordering of equal weights preferring rooms earlier in the array
    allEdges.Shuffle();
    allEdges = allEdges.OrderBy(edge => edge.Weight).ToList();

    HashSet<HashSet<Room>> disjointSets = new HashSet<HashSet<Room>>(rooms.Select(r => new HashSet<Room>() { r }));

    List<Edge> connectorEdges = new List<Edge>();
    for(int i = 0; i < allEdges.Count; i++) {
      // pick smallest edge
      var smallestEdge = allEdges[i];
      var disjointSetForRoom1 = disjointSets.First(set => set.Contains(smallestEdge.room1));
      var disjointSetForRoom2 = disjointSets.First(set => set.Contains(smallestEdge.room2));

      // this edge will create a cycle. For now, we ignore it (in the future it'd be nice to add a few small cycles)
      if (disjointSetForRoom1 == disjointSetForRoom2) {
        continue;
      } else {
        // this edge combines two new sets together! do that.
        connectorEdges.Add(smallestEdge);
        disjointSetForRoom1.UnionWith(disjointSetForRoom2);
        disjointSets.Remove(disjointSetForRoom2);

        // we're down to just one set, so we're done!
        if (disjointSets.Count == 1) {
          break;
        }
      }
    }

    if (disjointSets.Count > 1) {
      Debug.LogError($"There's still {disjointSets.Count} disjoint sets.");
    }

    // we now know how to connect the room. Connect them by 
    // carving out the connections
    foreach (var edge in connectorEdges) {
      foreach(var pos in floor.EnumerateLine(edge.connection.Item1, edge.connection.Item2)) {
        floor.Put(new Ground(pos));
        // floor.Put(new ThickBrush(pos));
      }
    }

    FloorUtils.NaturalizeEdges(floor);
    ShapeTransform WallNooks = new ShapeTransform(
      new int[3, 3] {
        {0, 0, 0},
        {1, 1, 1},
        {1, 1, 1}
      },
      FloorUtils.WallIs1,
      pos => MyRandom.value < 0.33f ? new Ground(pos) : null
    );
    WallNooks.ApplyWithRotations(floor);

    // foreach(var room in rooms) {
    //   // for each room, add thick brush to it
    //   foreach(var pos in floor.enumerateroom(room)) {
      foreach(var pos in floor.EnumerateFloor()) {
        if (floor.tiles[pos].CanBeOccupied() && floor.GetDiagonalAdjacentTiles(pos).OfType<Wall>().Any()) {
          floor.Put(new ThickBrush(pos));
        }
      }
    //   }
    // }

    int ThickBrushIs1(Tile tile) => tile.body is ThickBrush || tile is Wall ? 1 : 0;
    ShapeTransform FillInHoles = new ShapeTransform(
      new int[3, 3] {
        {0, 0, 0},
        {1, 0, 1},
        {1, 1, 1},
      },
      ThickBrushIs1,
      pos => MyRandom.value < 0.5f ? new ThickBrush(pos) : null
    );
    ShapeTransform FillInCorners = new ShapeTransform(
      new int[3, 3] {
        {1, 0, 0},
        {1, 0, 0},
        {1, 1, 1},
      },
      ThickBrushIs1,
      pos => MyRandom.value < 0.5f ? new ThickBrush(pos) : null
    );

    ShapeTransform ThickBrushBumps = new ShapeTransform(
      new int[3, 3] {
        {0, 0, 0},
        {1, 1, 1},
        {1, 1, 1},
      },
      ThickBrushIs1,
      pos => MyRandom.value < (1 - Mathf.Sqrt(0.5f)) ? new ThickBrush(pos) : null
    );

    FillInHoles.ApplyWithRotations(floor);
    FillInCorners.ApplyWithRotations(floor);
    ThickBrushBumps.ApplyWithRotations(floor);

    foreach(var room in rooms) {
      var chasmsPoses = floor.EnumerateRoom(room).Where(pos => floor.tiles[pos].CanBeOccupied());
      floor.PutAll(chasmsPoses.Select(pos => new Mist(pos, MistsHomeFloor.bag.GetRandom(), MyRandom.Range(1, 12))));
    }

// #if !experimental_chainfloors
//     // occasionally create a chasm in the multi room. Feels tight and cool, and makes the level harder.
//     if (MyRandom.value < 0.1) {
//       var depth2Room = Util.RandomPick(root.Traverse().Where(r => r.depth == 2));
//       Encounters.ChasmsAwayFromWalls1(floor, depth2Room);
//     }
// #endif

    return floor;
  }

  class Edge {
    public Room room1;
    public Room room2;
    public (Vector2Int, Vector2Int) connection;
    public int Weight => Util.manhattanDistance(connection.Item1 - connection.Item2);

    public override string ToString() {
      return $"{room1} <-> {room2}";
    }
  }

  protected override TileVisiblity RecomputeVisibilityFor(Tile t) {
    var newVisibility = TestVisibility(GameModel.main.player.pos, t.pos);
    if (t.isExplored && newVisibility == TileVisiblity.Unexplored) {
      return t.visibility;
    }
    return newVisibility;
  }

  public MultiRoomHomeFloor(int width, int height) : base(width, height) { }
}
