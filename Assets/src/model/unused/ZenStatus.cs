using System;

[Serializable]
[ObjectInfo("zen")]
class ZenStatus : StackingStatus, IActionCostModifier, IActionPerformedHandler, ITakeAnyDamageHandler, IDealAttackDamageHandler {
  public override StackingMode stackingMode => StackingMode.Max;
  public ZenStatus(int stacks) : base(stacks) {}

  public override string Info() => $"Your next {stacks} moves on a non-cleared Floor are Free Moves. Removed once you take or deal damage.";

  public ActionCosts Modify(ActionCosts costs) {
    if (actor.floor.EnemiesLeft() > 0) {
      costs[ActionType.MOVE] = 0f;
    }
    return costs;
  }

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (final.Type == ActionType.MOVE && actor.floor.EnemiesLeft() > 0) {
      stacks--;
    }
  }

  public void HandleTakeAnyDamage(int damage) {
    if (damage > 0) {
      Remove();
    }
  }

  public void HandleDealAttackDamage(int damage, Body target) {
    Remove();
  }
}
