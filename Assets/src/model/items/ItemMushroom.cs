using System;
using System.Collections.Generic;

class ItemMushroom : Item, IStackable {
  public ItemMushroom(int stacks) {
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
        Destroy(null);
      }
    }
  }

  public void Eat(Actor a) {
    if (a is Player player) {
      player.IncreaseFullness(0.05f);
    }
    stacks--;
  }

  public override List<ActorTask> GetAvailableTasks(Player player) {
    var actions = base.GetAvailableTasks(player);
    actions.Add(new GenericTask(player, Eat));
    return actions;
  }

  internal override string GetStats() => "Recover 5% hunger.";
}
