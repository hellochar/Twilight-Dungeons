using System;
using UnityEngine;

public interface ISteppable {
  /// implementors are responsible for modifying this
  float timeNextAction { get; set; }

  /// do not call this directly; should only be called from DoStep()
  float Step();

  /// Determines Actor order when multiple have the same timeNextAction.
  /// Lower numbers go first.
  /// Player has offset 10 (usually goes first).
  /// Generally ranges in [0, 100].
  float turnPriority { get; }
}

public static class ISteppableExtensions {
  public static float timeUntilTurn(this ISteppable s) => s.timeNextAction - GameModel.main.time;

  public static void DoStep(this ISteppable s) {
    float timeCost = s.Step();
    if (timeCost == 0 && s is AIActor) {
      Debug.LogWarning("Got a timeCost 0; adding a minimum step");
      timeCost = 0.01f;
    }
    s.timeNextAction += timeCost;
  }

  public static void CatchUpStep(this ISteppable s, float lastStepTime, float time) {
    if (s is Player || s is Plant) {
      // no ops
    } else {
      float jump = time - lastStepTime;
      // by default actors don't do anything; they just act as if they were paused
      // give 1 extra step so the Player can act first
      s.timeNextAction += jump + 1;
    }
  }
}