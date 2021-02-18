using System;
using UnityEngine;
using Random = System.Random;

[Serializable]
public static class MyRandom {
  public static float value => (float) generator.NextDouble();
  private static Random generator = new Random();
  public static void SetSeed(int seed) {
    Debug.Log("set seed to " + seed.ToString("X"));
    generator = new Random(seed);
  }

  /// min inclusive, max exclusive
  internal static int Range(int min, int max) {
    return generator.Next(min, max);
  }

  internal static int Next() {
    return generator.Next();
  }
}