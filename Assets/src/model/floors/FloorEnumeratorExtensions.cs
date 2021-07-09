using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class FloorEnumeratorExtensions {
  /// always starts right on the startPoint, and always ends right on the endPoint
  public static IEnumerable<Vector2Int> EnumerateLine(this Floor floor, Vector2Int startPoint, Vector2Int endPoint) {
    // Vector2 offset = endPoint - startPoint;
    // for (float t = 0; t <= offset.magnitude; t += 0.5f) {
    //   Vector2 point = startPoint + offset.normalized * t;
    //   Vector2Int p = new Vector2Int(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y));
    //   if (floor.InBounds(p)) {
    //     yield return p;
    //   }
    // }
    // if (floor.InBounds(endPoint)) {
    //   yield return endPoint;
    // }

    // https://stackoverflow.com/a/11683720
    var p = startPoint;
    int w = endPoint.x - startPoint.x;
    int h = endPoint.y - startPoint.y;
    int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
    if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
    if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
    if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
    int longest = Math.Abs(w);
    int shortest = Math.Abs(h);
    if (!(longest > shortest)) {
      longest = Math.Abs(h);
      shortest = Math.Abs(w);
      if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
      dx2 = 0;
    }
    int numerator = longest >> 1;
    for (int i = 0; i <= longest; i++) {
      yield return p;
      // putpixel(x, y, color);
      numerator += shortest;
      if (!(numerator < longest)) {
        numerator -= longest;
        p.x += dx1;
        p.y += dy1;
      } else {
        p.x += dx2;
        p.y += dy2;
      }
    }
  }

  public static IEnumerable<Vector2Int> EnumerateCircle(this Floor floor, Vector2Int center, float radius) {
    Vector2Int extent = new Vector2Int(Mathf.CeilToInt(radius), Mathf.CeilToInt(radius));
    foreach (var pos in floor.EnumerateRectangle(center - extent, center + extent + Vector2Int.one)) {
      if (Vector2Int.Distance(pos, center) < radius) {
        yield return pos;
      }
    }
  }

  /// max is exclusive
  public static IEnumerable<Vector2Int> EnumerateRectangle(this Floor floor, Vector2Int min, Vector2Int max) {
    min = Vector2Int.Max(min, floor.boundsMin);
    max = Vector2Int.Min(max, floor.boundsMax);
    for (int x = min.x; x < max.x; x++) {
      for (int y = min.y; y < max.y; y++) {
        yield return new Vector2Int(x, y);
      }
    }
  }

  public static IEnumerable<Vector2Int> EnumeratePerimeter(this Floor floor, int inset = 0) {
    // top edge, including top-left, excluding top-right
    for (int x = inset; x < floor.width - inset - 1; x++) {
      yield return new Vector2Int(x, inset);
    }
    // right edge
    for (int y = inset; y < floor.height - inset - 1; y++) {
      yield return new Vector2Int(floor.width - 1 - inset, y);
    }
    // bottom edge, now going right-to-left, now excluding bottom-left
    for (int x = floor.width - inset - 1; x > inset; x--) {
      yield return new Vector2Int(x, floor.height - 1 - inset);
    }
    // left edge
    for (int y = floor.height - inset - 1; y > inset; y--) {
      yield return new Vector2Int(inset, y);
    }
  }

  public static IEnumerable<Tile> EnumerateRoomTiles(this Floor floor, Room room, int extrude = 0) {
    return floor.EnumerateRoom(room, extrude).Select(x => floor.tiles[x]);
  }

  public static IEnumerable<Vector2Int> EnumerateRoom(this Floor floor, Room room, int extrude = 0) {
    Vector2Int extrudeVector = new Vector2Int(extrude, extrude);
    return floor.EnumerateRectangle(room.min - extrudeVector, room.max + Vector2Int.one + extrudeVector);
  }


  public static IEnumerable<Vector2Int> EnumerateFloor(this Floor floor) {
    return floor.EnumerateRectangle(floor.boundsMin, floor.boundsMax);
  }

  public static IEnumerable<Tile> BreadthFirstSearch(this Floor floor, Vector2Int startPos, Func<Tile, bool> predicate = null, bool randomizeNeighborOrder = true) {
    return floor.BreadthFirstSearch(new Vector2Int[] { startPos }, predicate, randomizeNeighborOrder);
  }

  public static IEnumerable<Tile> BreadthFirstSearch(this Floor floor, IEnumerable<Vector2Int> startPositions, Func<Tile, bool> predicate = null, bool randomizeNeighborOrder = true) {
    predicate = predicate ?? ((Tile t) => true);
    Queue<Tile> frontier = new Queue<Tile>(); // []
    HashSet<Tile> seen = new HashSet<Tile>(); // []
    foreach (var p in startPositions) {
      frontier.Enqueue(floor.tiles[p]); // frontier: [(3, 7)], seen: []
      seen.Add(floor.tiles[p]);
    }
    while (frontier.Any()) {
      Tile tile = frontier.Dequeue();
      yield return tile;
      var adjacent = floor.GetCardinalNeighbors(tile.pos).Except(seen).Where(predicate).ToList();
      if (randomizeNeighborOrder) {
        adjacent.Shuffle();
      }
      foreach (var next in adjacent) {
        frontier.Enqueue(next);
        seen.Add(next);
      }
    }
  }
}