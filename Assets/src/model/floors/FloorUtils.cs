using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class FloorUtils {
  public static List<Tile> TilesSortedByCorners(Floor floor, Room room) {
    var tiles = floor.EnumerateRoomTiles(room).ToList();
    tiles.Sort((x, y) => (int)Mathf.Sign(Vector2.Distance(y.pos, room.centerFloat) - Vector2.Distance(x.pos, room.centerFloat)));
    return tiles;
  }

  public static List<Tile> TilesSortedAwayFromFloorCenter(Floor floor, Room room) {
    var tiles = floor.EnumerateRoomTiles(room).ToList();
    tiles.OrderByDescending((t) => Vector2.Distance(t.pos, floor.center));
    return tiles;
  }

  internal static List<Tile> EmptyTilesInRoom(Floor floor, Room room) {
    return floor.EnumerateRoomTiles(room).Where(t => t.CanBeOccupied()).ToList();
  }

  internal static Tile EmptyTileNearestCenter(Floor floor, Room room) {
    var emptyTilesInRoom = FloorUtils.EmptyTilesInRoom(floor, room);
    emptyTilesInRoom.Sort((x, y) => Vector2Int.Distance(x.pos, room.center) < Vector2Int.Distance(y.pos, room.center) ? -1 : 1);
    return emptyTilesInRoom.FirstOrDefault();
  }

  // surround floor perimeter with walls
  public static void SurroundWithWalls(Floor floor) {
    for (int x = 0; x < floor.width; x++) {
      floor.Put(new Wall(new Vector2Int(x, 0)));
      floor.Put(new Wall(new Vector2Int(x, floor.height - 1)));
    }
    for (int y = 0; y < floor.height; y++) {
      floor.Put(new Wall(new Vector2Int(0, y)));
      floor.Put(new Wall(new Vector2Int(floor.width - 1, y)));
    }
  }

  ///<summary>Apply a natural look across the floor by smoothing both wall corners and space corners</summary>
  public static void NaturalizeEdges(Floor floor) {
    SMOOTH_ROOM_EDGES.ApplyWithRotations(floor);
    SMOOTH_WALL_EDGES.ApplyWithRotations(floor);
    MAKE_WALL_BUMPS.ApplyWithRotations(floor);
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