using System;
using System.Collections.Generic;
using UnityEngine;

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

  internal override string GetStats() => $"Recover 1% hunger per mushroom ({stacks}%).";
}

[ObjectInfo("mushroom", "Yummy")]
class PumpedUpStatus : StackingStatus, IBaseActionModifier, IActionCostModifier {
  public PumpedUpStatus(int stacks) : base(stacks) {}
  bool isModifying = false;

  public BaseAction Modify(BaseAction input) {
    if (input.Type == ActionType.ATTACK) {
      isModifying = true;
      stacks--;
    }
    return input;
  }

  public ActionCosts Modify(ActionCosts input) {
    if (isModifying) {
      input[ActionType.ATTACK] /= (1.5f);
    }
    return input;
  }

  public override string Info() => $"Your next {stacks} attacks are performed 50% faster!";
}