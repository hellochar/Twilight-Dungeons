using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Blocks your path. Leaves a debuff when you kill it")]
public class Slime : Destructible, IDeathHandler {
  public Slime(Vector2Int pos) : base(pos, 3) {
  }

  public void HandleDeath(Entity source) {
    // if (MyRandom.value < 0.1f) {
    //   GameModel.main.player.statuses.Add(new SlimyStatus(1, GameModel.main.currentFloor.depth));
    // }
    var floor = this.floor;
    floor.Put(new WallTrigger(pos));
    GameModel.main.EnqueueEvent(() => {
      var inventory = new Inventory(new ItemSlime());
      inventory.TryDropAllItems(floor, pos);
    });
#if experimental_retryondemand
    GameModel.main.EnqueueEvent(() => {
      GameModel.main.DrainEventQueue();
      Serializer.SaveMainToLevelStart();
    });
#endif
  }
}

[Serializable]
[ObjectInfo("slimed", description: "Purify at home to turn into Water!")]
public class ItemSlime : Item, IStackable, IStuckToInventory {
  public int stacks { get; set; }
  public int stacksMax => 8;

  public ItemSlime(int stacks) {
    this.stacks = stacks;
  }

  public ItemSlime() : this(1) { }

  [PlayerAction]
  public void Purify() {
    var player = GameModel.main.player;
    player.UseActionPointOrThrow();
    PurifyFree(player);
  }

  public void PurifyFree(Player player) {
    var water = 0;
    for(int i = 0; i < stacks; i++) {
      water += MyRandom.Range(13, 28);
    }
    player.water += water;
    Destroy();
  }

  public override List<MethodInfo> GetAvailableMethods(Player player) {
    // disallow dropping or destroying
    return new List<MethodInfo>();
    // var methods = base.GetAvailableMethods(player);
    // if (player.floor.depth == 0) {
    //   methods.Add(GetType().GetMethod("Purify"));
    // }
    // return methods;
  }
}

[Serializable]
class WallTrigger : Trigger, IActorLeaveHandler {
  public WallTrigger(Vector2Int pos) : base(pos) {}

  public void HandleActorLeave(Actor who) {
    if (who is Player && who.pos.x > pos.x) {
#if !experimental_useplantondeath
      KillSelf();
#endif
      who.floor.Put(new Wall(pos));
    }
  }
}

[System.Serializable]
[ObjectInfo("slimed", "You're filled with sticky slime! You take more damage.")]
internal class SlimyStatus : StackingStatus, IAttackDamageTakenModifier {
  public override bool isDebuff => true;
  public override StackingMode stackingMode => StackingMode.Add;
  public int depth;
  public SlimyStatus(int stacks, int depth) : base(stacks) {
    this.depth = depth;
  }

  public override string Info() => $"You take {stacks} more attack damage.";

  public int Modify(int input) {
    return input + stacks;
  }
}
