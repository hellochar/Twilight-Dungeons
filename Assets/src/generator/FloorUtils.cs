using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class FloorUtils {

  /// It sucks to walk down to a new level and immediately get
  /// constricted by a HangingVines, or just surrounded by enemies.
  /// Prevent these negative gameplay experiences.
  public static void TidyUpAroundStairs(Floor floor) {
    /// sometimes the Wall generators may put Walls right in the landing spot. Prevent that.
    if (floor.upstairs != null && !floor.tiles[floor.upstairs.landing].CanBeOccupied()) {
      floor.Put(new HardGround(floor.upstairs.landing));
    }
    if (floor.downstairs != null && !floor.tiles[floor.downstairs.landing].CanBeOccupied()) {
      floor.Put(new HardGround(floor.downstairs.landing));
    }

    // Clear hanging vines and move immediately adjacent enemies
    if (floor.upstairs != null) {
      foreach (var tile in floor.GetAdjacentTiles(floor.upstairs.pos)) {
        if (tile.grass is HangingVines) {
          // do NOT call Kill(); we don't want the vine whip to drop.
          floor.Remove(tile.grass);
        }
        if (tile.actor != null) {
          // TODO pick a spot that graspers can inhabit
          var newSpot = Util.RandomPick(floor.EnumerateRoomTiles(floor.root).Where((x) => x.CanBeOccupied()));
          // move the actor to a different spot in the map
          tile.actor.pos = newSpot.pos;
        }
      }
    }

    // ensure that the player can't spawn on top of a wall
    if (floor.tiles[floor.upstairsPos] is Chasm) {
      floor.Put(new HardGround(floor.upstairsPos));
    }
    floor.Put(new HardGround(floor.startPos));

    if (floor.tiles[floor.downstairsPos] is Chasm) {
      floor.Put(new HardGround(floor.downstairsPos));
    }
    floor.Put(new HardGround(floor.downstairsPos + Vector2Int.left));

    /// HACK I've decided that grass shouldn't *initially spawn* right next to the stairs,
    /// but that post-generation, grass should still be able. So we'll now only use HardGround
    /// during generation, and then replace it with normal ground
    foreach (var pos in floor.EnumerateFloor()) {
      if (floor.tiles[pos] is HardGround) {
        floor.Put(new Ground(pos));
      }
    }
  }

  public static List<Tile> TilesSortedByCorners(Floor floor, Room room) {
    var tiles = floor.EnumerateRoomTiles(room).ToList();
    tiles.Sort((x, y) => (int)Mathf.Sign(Vector2.Distance(y.pos, room.centerFloat) - Vector2.Distance(x.pos, room.centerFloat)));
    return tiles;
  }

  internal static List<Tile> EmptyTilesInRoom(Floor floor, Room room) {
    return floor.EnumerateRoomTiles(room).Where(t => t.CanBeOccupied() && !(t is Downstairs || t is Upstairs)).ToList();
  }

  internal static List<Tile> TilesFrom(Floor floor, Room room, Vector2 pos) {
    return FloorUtils
      .EmptyTilesInRoom(floor, room)
      .OrderBy((t) => Vector2.Distance(t.pos, pos))
      .ToList();
  }

  public static List<Tile> TilesAwayFrom(Floor floor, Room room, Vector2 pos) {
    var tiles = TilesFrom(floor, room, pos);
    tiles.Reverse();
    return tiles;
  }

  internal static List<Tile> TilesFromCenter(Floor floor, Room room) {
    return TilesFrom(floor, room, room.centerFloat);
  }

  public static List<Tile> TilesAwayFromCenter(Floor floor, Room room) {
    var tiles = TilesFromCenter(floor, room);
    tiles.Reverse();
    return tiles;
  }

  public static IEnumerable<Vector2Int> Line3x3(Floor floor, Vector2Int start, Vector2Int end) {
      return floor.EnumerateLine(start, end).SelectMany((pos) => floor.GetAdjacentTiles(pos).Select(t => t.pos));
  }

  // surround floor perimeter with walls
  public static void SurroundWithWalls(Floor floor) {
    foreach (var p in floor.EnumeratePerimeter()) {
      floor.Put(new Wall(p));
    }
  }

  // Replaces unwalkable tiles (walls and chasms) with Ground
  public static void CarveGround(Floor floor, IEnumerable<Vector2Int> points = null) {
    if (points == null) {
      points = floor.EnumerateFloor();
    }
    foreach (var pos in points) {
      var isUnwalkable = (floor.tiles[pos]?.BasePathfindingWeight() ?? 0) == 0;
      if (isUnwalkable) {
        floor.Put(new Ground(pos));
      }
    }
  }

  public static IEnumerable<Tile> Clusters(Floor floor, Vector2Int inset, int num) {
    var pos = MyRandom.Range(floor.boundsMin + inset, floor.boundsMax - inset);
    // var numTiles = (float) floor.width * floor.height;
    // var num = MyRandom.Range(Mathf.RoundToInt(numTiles / 8), Mathf.RoundToInt(numTiles / 4));
    // var num = (int)(numTiles / 3);
    return floor.BreadthFirstSearch(pos).Take(num);
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