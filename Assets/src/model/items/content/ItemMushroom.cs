using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[ObjectInfo("mushroom", "They have such an interesting taste!")]
class ItemMushroom : Item, IStackable, IEdible {
  public ItemMushroom(int stacks) {
    this.stacks = stacks;
  }
  public int stacksMax => 100;

  private int _stacks;
  public int stacks {
    get => _stacks;
    set {
      if (value < 0) {
        throw new ArgumentException("Setting negative stack!" + this + " to " + value);
      }
      _stacks = value;
      if (_stacks == 0) {
        Destroy();
      }
    }
  }

  public void Eat(Actor a) {
    a.statuses.Add(new PumpedUpStatus(stacks));
    stacks = 0;
  }

  internal override string GetStats() => $"Eat all to make your next {stacks} attacks twice as fast.";
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