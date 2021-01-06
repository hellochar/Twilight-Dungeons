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
    SetTasks(new SleepTask(this));
  }

  private void HandleDeath() {
    var floor = this.floor;
    var pos = this.pos;
    GameModel.main.EnqueueEvent(() => inventory.TryDropAllItems(floor, pos));
  }

  private static int MaxRetries = 2;

  public void SetAI(IEnumerator<ActorTask> ai) {
    this.ai = ai;
    ClearTasks();
  }

  protected override float Step() {
    // the first step will likely be "no action" so retries starts at -1
    for (int retries = -1; retries < MaxRetries; retries++) {
      try {
        return base.Step();
      } catch (NoActionException) {
        if (ai.MoveNext()) {
          SetTasks(ai.Current);
        } else {
          SetTasks(new WaitTask(this, 99999));
          throw new System.Exception("AI Enumerator ended!" + this);
        }
      }
    }
    Debug.LogWarning(this + " reached MaxSkippedActions!");
    SetTasks(new WaitTask(this, 1));
    return base.Step();
  }
}
