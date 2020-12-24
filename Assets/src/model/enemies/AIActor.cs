using System;
using System.Collections.Generic;
using UnityEngine;

/// An actor whose actions are controlled by some sort of AI.
/// This AI decides what actions the actor takes.
/// TODO we should use composition for this instead, eventually
public class AIActor : Actor {
  protected IEnumerator<ActorTask> ai;
  public Inventory inventory = new Inventory(1);
  public AIActor(Vector2Int pos) : base(pos) {
    OnDeath += HandleDeath;
  }

  private void HandleDeath() {
    var floor = this.floor;
    var pos = this.pos;
    GameModel.main.EnqueueEvent(() => inventory.DropRandomlyOntoFloorAround(floor, pos));
  }

  private static int MaxRetries = 3;

  public void SetAI(IEnumerator<ActorTask> ai) {
    this.ai = ai;
    ClearTasks();
  }

  protected override float Step() {
    for (int retries = 0; retries < MaxRetries; retries++) {
      try {
        return base.Step();
      } catch (NoActionException) {
        if (ai.MoveNext()) {
          SetTasks(ai.Current);
        } else {
          throw new System.Exception("AI Enumerator ended!");
        }
      }
    }
    Debug.LogWarning(this + " reached MaxSkippedActions!");
    SetTasks(new WaitTask(this, 1));
    return base.Step();
  }
}
