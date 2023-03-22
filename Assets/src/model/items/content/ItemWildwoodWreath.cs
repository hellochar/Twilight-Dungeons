using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class ItemWildwoodWreath : EquippableItem, IBodyMoveHandler {
  public static int yieldCost = 2;

  public ItemWildwoodWreath(int stacks) : base(stacks) {}

  public override EquipmentSlot slot => EquipmentSlot.Headwear;
  public override int stacksMax => int.MaxValue;
  // public override bool disjoint => true;

  public void HandleMove(Vector2Int newPos, Vector2Int oldPos) {
    var target = Util.RandomPick(player.floor.AdjacentActors(player.pos).Where(a => a.faction != Faction.Ally && !a.statuses.Has<ConfusedStatus>()));
    if (target != null) {
      target.statuses.Add(new ConfusedStatus(5));
      stacks--;
    }
  }

  internal override string GetStats() => "Moving applies the Confused Status to a random adjacent creature for 5 turns.\nConfused enemies move randomly and do not attack.";
}

[System.Serializable]
[ObjectInfo("confused")]
public class ConfusedStatus : StackingStatus, IBaseActionModifier {
  public override StackingMode stackingMode => StackingMode.Max;
  public override string Info() => $"Your next {stacks} turns must be spent moving in a random direction.";

  public ConfusedStatus(int stacks) : base(stacks) {}

  public BaseAction Modify(BaseAction input) {
    stacks--;
    if (input.Type == ActionType.MOVE || input.Type == ActionType.ATTACK) {
      return MoveRandomlyTask.GetRandomMove(input.actor);
    } else {
      return input;
    }
  }
}