using System;
using UnityEngine;

[Serializable]
class PseudoRandomDistribution {
  // Copied from https://dota2.fandom.com/wiki/Random_distribution - each index is 5% higher chance of P
  public static float[] cValuesPerFivePercent = new float[] { 0.0038f, 0.015f, 0.032f, 0.056f, 0.085f, 0.12f, 0.16f, 0.20f, 0.25f, 0.30f, 0.36f, 0.42f, 0.48f, 0.57f, 0.67f, 0.75f, 0.82f, 0.89f, 0.95f };
  private int timesSinceLastProc = 0;
  private float C;

  // index 0 is 5%, index 1 is 10%, 2 is 15%, etc.
  public PseudoRandomDistribution(float C) {
    this.C = C;
  }

  public bool Test() {
    var pNow = (timesSinceLastProc + 1) * C;
    Debug.Log(timesSinceLastProc + ", " + pNow);
    var result = MyRandom.value < pNow;
    if (result) {
      timesSinceLastProc = 0;
      return true;
    } else {
      timesSinceLastProc++;
      return false;
    }
  }

  // https://gaming.stackexchange.com/a/178681
  public static decimal CfromP(decimal p) {
    decimal Cupper = p;
    decimal Clower = 0m;
    decimal Cmid;
    decimal p1;
    decimal p2 = 1m;
    while (true) {
      Cmid = (Cupper + Clower) / 2m;
      p1 = PfromC(Cmid);
      if (Math.Abs(p1 - p2) <= 0m) break;

      if (p1 > p) {
        Cupper = Cmid;
      } else {
        Clower = Cmid;
      }

      p2 = p1;
    }

    return Cmid;
  }

  public static decimal PfromC(decimal C) {
    decimal pProcOnN = 0m;
    decimal pProcByN = 0m;
    decimal sumNpProcOnN = 0m;

    int maxFails = (int)Math.Ceiling(1m / C);
    for (int N = 1; N <= maxFails; ++N) {
      pProcOnN = Math.Min(1m, N * C) * (1m - pProcByN);
      pProcByN += pProcOnN;
      sumNpProcOnN += N * pProcOnN;
    }

    return (1m / sumNpProcOnN);
  }
}
