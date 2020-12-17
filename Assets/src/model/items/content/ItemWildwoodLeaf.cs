using System;
using System.Linq;

class ItemWildwoodLeaf : Item, IStackable, IEdible {
  public ItemWildwoodLeaf(int stacks) {
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

  public void Eat(Actor a) {
    a.statuses.Add(new StatusWild());
    stacks--;
  }

  internal override string GetStats() => "Apply the Wild status for 15 turns.";
}

internal class StatusWild : StackingStatus, IActionCostModifier {
  public StatusWild() : base(25) {}

  public override string Info() => $"Move twice as fast.";

  public ActionCosts Modify(ActionCosts input) {
    input[ActionType.MOVE] /= 2;
    return input;
  }

  public override void Step() {
    stacks--;
  }
}
