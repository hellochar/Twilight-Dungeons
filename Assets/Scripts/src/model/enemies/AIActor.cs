using System.Collections.Generic;
using UnityEngine;

/// An actor whose actions are controlled by some sort of AI.
/// This AI decides what actions the actor takes.
/// TODO we should use composition for this instead, eventually
public class AIActor : Actor {
  protected IEnumerator<ActorAction> ai;
  public AIActor(Vector2Int pos) : base(pos) { }

  private static int MaxRetries = 3;

  protected override float Step() {
    for (int retries = 0; retries < MaxRetries; retries++) {
      try {
        return base.Step();
      } catch (NoActionException) {
        if (ai.MoveNext()) {
          SetActions(ai.Current);
        } else {
          throw new System.Exception("AI Enumerator ended!");
        }
      }
    }
    Debug.LogWarning(this + " reached MaxSkippedActions!");
    SetActions(new WaitAction(this, 1));
    return base.Step();
  }
}
