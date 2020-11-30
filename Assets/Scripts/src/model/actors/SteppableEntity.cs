using System;
using UnityEngine;

public abstract class SteppableEntity : Entity {
  /// implementors are responsible for modifying this
  public virtual float timeNextAction { get; protected set; }

  /// Determines Actor order when multiple have the same timeNextAction.
  /// Lower numbers go first.
  /// Player has offset 10 (usually goes first).
  /// Generally ranges in [0, 100].
  internal virtual float turnPriority => 50;

  public event Action OnPreStep;
  public event Action<float> OnStepped;

  /// do not call this directly; should only be called from DoStep()
  protected abstract float Step();

  public void DoStep() {
    OnPreStep?.Invoke();
    float timeCost = Step();
    if (timeCost == 0) {
      Debug.LogWarning("Got a timeCost 0; adding a minimum step");
      timeCost = 0.01f;
    }
    timeNextAction += timeCost;
    OnStepped?.Invoke(timeCost);
  }

  public void CatchUpStep(float newTime) {
    // by default actors don't do anything; they just act as if they were paused
    this.timeNextAction = Mathf.Max(this.timeNextAction, newTime);
  }
}
