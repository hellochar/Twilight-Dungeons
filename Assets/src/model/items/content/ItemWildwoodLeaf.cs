using System;
using System.Linq;

[Serializable]
class ItemWildwoodLeaf : Item, IEdible {
  public static int yieldCost = 2;
  public override int stacksMax => int.MaxValue;

  public void Eat(Actor a) {
    a.statuses.Add(new StatusWild());
    stacks--;
  }

  internal override string GetStats() => "Apply the Wild status for 15 turns, doubling your movespeed.";
}

[System.Serializable]
internal class StatusWild : StackingStatus, IActionCostModifier {
  public StatusWild(int stacks) : base(stacks) {}
  public StatusWild() : this(15) {}

  public override string Info() => $"Move twice as fast.";

  public ActionCosts Modify(ActionCosts input) {
    input[ActionType.MOVE] /= 2;
    return input;
  }

  public override void Step() {
    stacks--;
  }
}
