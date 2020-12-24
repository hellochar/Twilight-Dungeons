using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Util {

  public static Vector2 getXY(Vector3 v3) {
    return new Vector2(v3.x, v3.y);
  }

  public static Vector3 withZ(Vector2 vector2, float z = 0) {
    return new Vector3(vector2.x, vector2.y, z);
  }

  // public static Vector3 LerpOrJump(Vector3 a, Vector3 b, float t, float threshold) {
  // }

  public static int manhattanDistance(Vector2Int vector) {
    return Mathf.Abs(vector.x) + Mathf.Abs(vector.y);
  }

  public static Tile GetVisibleTileAt(Vector3 screenPoint) {
    Vector3 worldTarget = Camera.main.ScreenToWorldPoint(screenPoint);
    Vector2Int target = new Vector2Int(Mathf.RoundToInt(worldTarget.x), Mathf.RoundToInt(worldTarget.y));
    Floor currentFloor = GameModel.main.currentFloor;
    target.Clamp(currentFloor.boundsMin, currentFloor.boundsMax - new Vector2Int(1, 1));
    Tile tile = currentFloor.tiles[target.x, target.y];
    if (tile != null && tile.visibility != TileVisiblity.Unexplored) {
      return tile;
    }
    return null;
  }

  public static Vector2Int[] AdjacentDirections = new Vector2Int[] {
    new Vector2Int(-1, -1),
    new Vector2Int(-1,  0),
    new Vector2Int(-1, +1),

    new Vector2Int(0, -1),
    // new Vector2Int(0,  0),
    new Vector2Int(0, +1),

    new Vector2Int(+1, -1),
    new Vector2Int(+1,  0),
    new Vector2Int(+1, +1),
  };

  internal static Vector2Int RandomAdjacentDirection() {
    return RandomPick(AdjacentDirections);
  }

  public static T RandomPick<T>(IEnumerable<T> items) {
    if (items.Count() == 0) {
      return default(T);
    }
    return items.ElementAt(UnityEngine.Random.Range(0, items.Count()));
  }

  public static void Shuffle<T>(this IList<T> list) {
    int n = list.Count;
    while (n > 1) {
      n--;
      int k = UnityEngine.Random.Range(0, n + 1);
      T value = list[k];
      list[k] = list[n];
      list[n] = value;
    }
  }

  public static float MapLinear(float value, float from1, float to1, float from2, float to2) {
    return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
  }

  public static string WithSpaces(string capitalCaseString) {
    return string.Concat(capitalCaseString.Select(x => Char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
  }

  /// <summary>Create a new 3x3 chunk that's the old one rotated 90 degrees counterclockwise (because we're in a right-handed coordinate system)</summary>
  public static int[,] Rotate90(int[,] chunk) {
    int i1 = chunk[0, 0];
    int i2 = chunk[0, 1];
    int i3 = chunk[0, 2];

    int i4 = chunk[1, 0];
    int i5 = chunk[1, 1];
    int i6 = chunk[1, 2];

    int i7 = chunk[2, 0];
    int i8 = chunk[2, 1];
    int i9 = chunk[2, 2];

    return new int[3, 3] {
      {i3, i6, i9},
      {i2, i5, i8},
      {i1, i4, i7}
    };
  }

  public static void FillChunkCenteredAt(Floor floor, int x, int y, ref int[,] chunk, Func<Vector2Int, int> func, int outOfBoundsValue) {
    for (int dx = -1; dx <= 1; dx++) {
      for (int dy = -1; dy <= 1; dy++) {
        Vector2Int pos = new Vector2Int(x + dx, y + dy);
        if (floor.InBounds(pos)) {
          chunk[dx + 1, dy + 1] = func(pos);
        } else {
          chunk[dx + 1, dy + 1] = outOfBoundsValue;
        }
      }
    }
  }

  public static T ClampPick<T>(int index, params T[] values) {
    return values[Mathf.Clamp(index, 0, values.Length - 1)];
  }
}
