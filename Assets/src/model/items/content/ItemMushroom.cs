using System;
using System.Collections.Generic;
using UnityEngine;

[ObjectInfo("mushroom", "Don't pick them all! Mushrooms spread to nearby squares on their own.")]
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
    a.statuses.Add(new WellFedStatus(10 * stacks));
    stacks = 0;
  }

  internal override string GetStats() => $"Recover 1% hunger per mushroom ({stacks}%).";
}

[ObjectInfo("mushroom", "Yummy")]
class WellFedStatus : StackingStatus, IBaseActionModifier {
  private int turnsLeft = 50;

  public WellFedStatus(int stacks) : base(stacks) {}

  public BaseAction Modify(BaseAction input) {
    turnsLeft--;
    if (turnsLeft <= 0) {
      input.actor.Heal(1);
      turnsLeft = 50;
      stacks--;
    }
    return input;
  }

  public override string Info() => $"Every 50 turns ({turnsLeft} left), heal 1 HP.";
}