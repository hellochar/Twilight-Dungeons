using System;
using System.Collections.Generic;

class ItemBerries : Item, IStackable {
  public ItemBerries(int stacks) {
    this.stacks = stacks;
  }

  public int stacksMax => 10;

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

  [PlayerAction]
  public void Use(Actor a) {
    a.Heal(3);
    if (a is Player player) {
      player.IncreaseFullness(0.05f);
    }
    stacks--;
  }

  internal override string GetStats() => "Heals 3 HP.\nRecover 5% hunger.";
}
