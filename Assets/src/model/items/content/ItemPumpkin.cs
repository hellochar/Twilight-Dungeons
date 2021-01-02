[ObjectInfo("pumpkin", "Round and smooth and full of raw calories.")]
public class ItemPumpkin : Item, IEdible {
  public ItemPumpkin() {}

  public void Eat(Actor a) {
    a.statuses.Add(new WellFedStatus(4));
    a.floor.Put(new ItemOnGround(a.pos, new ItemPumpkinHelmet(), a.pos));
    Destroy();
  }

  internal override string GetStats() => "Gives you the Well Fed (x4) buff.\nMakes a nice helmet after you eat it.";
}

[ObjectInfo("pumpkin", "Yummy")]
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