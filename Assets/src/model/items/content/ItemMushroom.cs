using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[ObjectInfo("mushroom", "They have such an interesting taste!")]
class ItemMushroom : Item, IEdible {
  public ItemMushroom(int stacks) : base(stacks) {}
  public override int stacksMax => 100;

  public void Eat(Actor a) {
    a.statuses.Add(new PumpedUpStatus(stacks));
    stacks = 0;
  }

  internal override string GetStats() => $"Get {stacks} stacks of the Pumped Up status.";
}

[System.Serializable]
[ObjectInfo("mushroom", "Yummy")]
class PumpedUpStatus : StackingStatus, IActionCostModifier, IActionPerformedHandler {
  public PumpedUpStatus(int stacks) : base(stacks) {}

  public ActionCosts Modify(ActionCosts input) {
    input[ActionType.ATTACK] /= 2;
    return input;
  }

  public override string Info() => $"Your next {stacks} attacks are twice as fast!";

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (final.Type == ActionType.ATTACK) {
      stacks--;
    }
  }
}