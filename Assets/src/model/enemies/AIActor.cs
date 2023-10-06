using System;
using System.Collections.Generic;
using UnityEngine;

/// An actor whose actions are controlled by some sort of AI.
[Serializable]
public abstract class AIActor : Actor, IDeathHandler {
  public Inventory inventory = new Inventory(3);
  private AI aiOverride;
  public AIActor(Vector2Int pos) : base(pos) {
    SetTasks(new SleepTask(this));
  }

  public virtual void HandleDeath(Entity source) {
    var floor = this.floor;
    var pos = this.pos;
    GameModel.main.EnqueueEvent(() => inventory.TryDropAllItems(floor, pos));
  }

  private static int MaxRetries = 2;

  public void SetAI(AI ai) {
    this.aiOverride = ai;
    ClearTasks();
  }

  protected abstract ActorTask GetNextTask();

  public override float Step() {
    // the first step will likely be "no action" so retries starts at -1
    for (int retries = -1; retries < MaxRetries; retries++) {
      try {
        return base.Step();
      } catch (NoActionException) {
        if (aiOverride != null) {
          SetTasks(aiOverride.GetNextTask());
        } else {
          SetTasks(GetNextTask());
        }
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
