using System;
using System.Collections.Generic;
using UnityEngine;

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
    if (a is Player p) {
      var amountToEat = Mathf.Clamp(Mathf.FloorToInt((1.0f - p.fullness) * 100), 0, stacks);
      p.IncreaseFullness(0.01f * amountToEat);
      stacks -= amountToEat;
    }
  }

  internal override string GetStats() => $"Recover 1% hunger per mushroom ({stacks}%).";
}
