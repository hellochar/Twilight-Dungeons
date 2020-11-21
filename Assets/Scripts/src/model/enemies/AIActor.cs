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

  void HandlePreStep() {
    if (action == null) {
      do {
        ai.MoveNext();
      } while (ai.Current.IsDone());
      SetActions(ai.Current);
    }
  }
}
