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
}