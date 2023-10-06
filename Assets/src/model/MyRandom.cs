using System;
using UnityEngine;
using Random = System.Random;

[Serializable]
public static class MyRandom {
  public static float value => (float) generator.NextDouble();
  private static Random generator = new Random();
  public static void SetSeed(int seed) {
    generator = new Random(seed);
  }

  /// min inclusive, max exclusive
  internal static int Range(int min, int max) {
    if (min > max) {
      min = max;
    }
    return generator.Next(min, max);
  }

  internal static Vector2Int Range(Vector2Int min, Vector2Int max) {
    return new Vector2Int(Range(min.x, max.x), Range(min.y, max.y));
  }

  internal static int Next() {
    return generator.Next();
  }

  public static int RandRound(float v) {
    // for 3.7, we want a 70% chance for 4, 30% chance for 3
    // for 3.2, we want a 20% chance for 4, 80% chance for 3

    float mod = v % 1;
    int floor = Mathf.FloorToInt(v);
    int ceil = Mathf.CeilToInt(v);
    if (mod == 0) {
      return floor;
    }
    
    if (value < mod) {
      return ceil;
    } else {
      return floor;
    }
  }
}