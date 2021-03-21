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

  internal static int Next() {
    return generator.Next();
  }
}