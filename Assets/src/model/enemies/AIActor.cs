using System;
using System.Collections.Generic;
using UnityEngine;

/// An actor whose actions are controlled by some sort of AI.
/// This AI decides what actions the actor takes.
/// TODO we should use composition for this instead, eventually
public class AIActor : Actor, IDeathHandler {
  protected IEnumerator<ActorTask> ai;
  public Inventory inventory = new Inventory(1);
  public AIActor(Vector2Int pos) : base(pos) {
    SetTasks(new SleepTask(this));
  }

  public virtual void HandleDeath() {
    var floor = this.floor;
    var pos = this.pos;
    GameModel.main.EnqueueEvent(() => inventory.TryDropAllItems(floor, pos));
  }

  private static int MaxRetries = 2;

  public void SetAI(IEnumerator<ActorTask> ai) {
    this.ai = ai;
    ClearTasks();
  }

  private ActorTask MoveAIEnumerator() {
    if (ai.MoveNext()) {
      return ai.Current;
    } else {
      Debug.LogError("AI Enumerator ended!" + this);
      return new WaitTask(this, 99999);
    }
  }

  public override float Step() {
    // the first step will likely be "no action" so retries starts at -1
    for (int retries = -1; retries < MaxRetries; retries++) {
      try {
        return base.Step();
      } catch (NoActionException) {
        SetTasks(MoveAIEnumerator());
      }
    }
    Debug.LogWarning(this + " reached MaxSkippedActions!");
    SetTasks(new WaitTask(this, 1));
    return base.Step();
  }

  /// this is technically correct but it created a bunch of errors; fix later
  // protected override void GoToNextTask() {
  //   if (taskQueue.Count == 1 && task.IsDoneOrForceOpen()) {
  //     // queue up another task from the list
  //     var task = MoveAIEnumerator();
  //     taskQueue.Add(task);
  //   }
  //   base.GoToNextTask();
  // }
}
