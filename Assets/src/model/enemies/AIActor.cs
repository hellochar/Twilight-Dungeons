using System;
using UnityEngine;

/// An actor whose actions are controlled by some sort of AI.
[Serializable]
public abstract class AIActor : Actor, IDeathHandler {
  public Inventory inventory = new Inventory(2);
  private AI aiOverride;
  public AIActor(Vector2Int pos) : base(pos) {
    SetTasks(new SleepTask(this));
#if experimental_fertilizer
    inventory.AddItem(new ItemFertilizer(GetType()));
#endif
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

  [PlayerAction]
  public void PickUp() {
    var player = GameModel.main.player;
    bool bSuccess = player.inventory.AddItem(new ItemPlaceableEntity(this), this);
    if (bSuccess) {
      // we're *not* killing the entity
      floor.Remove(this);
    }
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
