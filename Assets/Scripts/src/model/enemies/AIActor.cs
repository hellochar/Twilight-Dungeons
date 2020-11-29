using System.Collections.Generic;
using UnityEngine;
/// An actor whose actions are controlled by some sort of AI.
/// This AI decides what actions the actor takes.
/// TODO we should use composition for this instead, eventually
public class AIActor : Actor {
  protected IEnumerator<ActorAction> ai;
  public AIActor(Vector2Int pos) : base(pos) {
    OnPreStep += HandlePreStep;
  }

  private static int MaxSkippedActions = 3;

  void HandlePreStep() {
    if (action == null) {
      var i = 0;
      do {
        ai.MoveNext();
        i++;
      } while (ai.Current.IsDone() && i < MaxSkippedActions);
      if (i == MaxSkippedActions) {
        Debug.LogWarning("" + this + " reached MaxSkippedActions!");
        SetActions(new WaitAction(this, 1));
      } else {
        SetActions(ai.Current);
      }
    }
  }
}
