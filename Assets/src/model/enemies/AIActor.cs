using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using static YieldContribution;

/// An actor whose actions are controlled by some sort of AI.
[Serializable]
public abstract class AIActor : Actor, IDeathHandler, IDaySteppable, IInteractableInventory {
  public Inventory inventory = new Inventory(2);
  private AI aiOverride;

  private Inventory stomach = new Inventory(1).AllowDragAndDrop();
  Inventory IInteractableInventory.inventory => stomach;
  public Inventory processedInventory = new Inventory(1);

  public AIActor(Vector2Int pos) : base(pos) {
    SetTasks(new SleepTask(this));
#if experimental_fertilizer
    inventory.AddItem(new ItemFertilizer(GetType()));
#endif
    stomach.OnItemAdded += HandleItemAdded;
    stomach.OnItemRemoved += HandleItemRemoved;
  }

  [OnDeserialized]
  void HandleDeserialized() {
    stomach.OnItemAdded += HandleItemAdded;
    stomach.OnItemRemoved += HandleItemRemoved;
  }

  private void HandleItemRemoved(Item obj) {
    processedInventory.RemoveItem(processedInventory[0]);
  }

  private void HandleItemAdded(Item item, Entity arg2) {
    processedInventory.AddItem(this.GetHomeItem());
  }

  public override string description => floor is HomeFloor ?
    stomach[0] == null ? 
    $"Feed an item to the {displayName}." :
    $"Digesting! Come back tomorrow."
  : base.description;

  public virtual void HandleDeath(Entity source) {
    var floor = this.floor;
    var pos = this.pos;
    GameModel.main.EnqueueEvent(() => inventory.TryDropAllItems(floor, pos));
    // if (faction == Faction.Enemy) {
    //   GameModel.main.EnqueueEvent(() => {
    //     var iMatter = new OrganicMatterOnGround(pos);
    //     floor.Put(iMatter);
    //   });
    // }
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
    player.UseActionPointOrThrow();
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

  public static YieldContributionRule[] BaseAIActorContributionRules => new YieldContributionRule[] {
    // AgeYieldContribution,
    // NearGrassYieldContribution,
    SoilWateredYieldContribution,
    SoilNutrientYieldContribution
  };

  public virtual YieldContributionRule[] contributionRules => BaseAIActorContributionRules;

  public List<YieldContribution> latestContributions = new List<YieldContribution>();

  public virtual void StepDay() {
    if (processedInventory[0] != null) {
      processedInventory.TryDropAllItems(floor, pos);
      stomach[0].stacks--;
    }

    // int yield = YieldContributionUtils.Recompute(this, contributionRules, this.latestContributions);

    // int itemCost = YieldContributionUtils.GetCost(item);
    // int stacks = yield / itemCost;
    
    // if (stacks > 0) {
    //   // our yield is high enough, drop an item
    //   item.stacks = stacks;
    //   floor.Put(new ItemOnGround(pos, item));
    // }
  }
}
