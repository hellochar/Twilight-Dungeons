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

  internal static List<Tile> EmptyTilesInRoom(Floor floor, Room room) {
    return floor.EnumerateRoomTiles(room).Where(t => t.CanBeOccupied() && !(t is Downstairs || t is Upstairs)).ToList();
  }

  internal static List<Tile> TilesFromCenter(Floor floor, Room room) {
    return FloorUtils
      .EmptyTilesInRoom(floor, room)
      .OrderBy((t) => Vector2.Distance(t.pos, room.centerFloat))
      .ToList();
  }

  public static List<Tile> TilesAwayFromCenter(Floor floor, Room room) {
    var tiles = TilesFromCenter(floor, room);
    tiles.Reverse();
    return tiles;
  }

  // surround floor perimeter with walls
  public static void SurroundWithWalls(Floor floor) {
    foreach (var p in floor.EnumeratePerimeter()) {
      floor.Put(new Wall(p));
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