using System;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Blocks your path. Leaves a debuff when you kill it")]
public class Slime : Destructible, IDeathHandler {
  public Slime(Vector2Int pos) : base(pos, 3) {
  }

  public void HandleDeath(Entity source) {
    var inventory = new Inventory(new ItemSlime());
    inventory.TryDropAllItems(floor, pos);
    if (MyRandom.value < 0.25f) {
      GameModel.main.player.statuses.Add(new SlimyStatus(1, GameModel.main.currentFloor.depth));
    }
    floor.Put(new WallTrigger(pos));
[Serializable]
[ObjectInfo("slimed", description: "Purify at home to turn into Water!")]
public class ItemSlime : Item, IStackable {
  public int stacks { get; set; }
  public int stacksMax => 3;

  public ItemSlime(int stacks) {
    this.stacks = stacks;
  }

  public ItemSlime() : this(1) { }

  public void Purify(Player player) {
    if (player.actionPoints < 1) {
      throw new CannotPerformActionException("Need an action point!");
    }
    player.actionPoints--;
    player.water += MyRandom.Range(25, 36) * stacks;
    Destroy();
  }

  public override List<MethodInfo> GetAvailableMethods(Player player) {
    var methods = base.GetAvailableMethods(player);
    if (player.floor.depth == 0) {
      methods.Add(GetType().GetMethod("Purify"));
    }
    return methods;
  }
}

[Serializable]
class WallTrigger : Trigger, IActorLeaveHandler {
  public WallTrigger(Vector2Int pos) : base(pos) {}

  public void HandleActorLeave(Actor who) {
    if (who is Player) {
      KillSelf();
      who.floor.Put(new Wall(pos));
#if experimental_retryondemand
      GameModel.main.EnqueueEvent(() => {
        GameModel.main.DrainEventQueue();
        Serializer.SaveMainToLevelStart();
      });
#endif
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
