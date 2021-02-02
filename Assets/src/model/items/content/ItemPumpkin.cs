[System.Serializable]
[ObjectInfo("pumpkin", "Round and smooth and full of raw calories.")]
public class ItemPumpkin : Item, IEdible {
  public ItemPumpkin() {}

  public void Eat(Actor a) {
    a.statuses.Add(new WellFedStatus(50));
    a.floor.Put(new ItemOnGround(a.pos, new ItemPumpkinHelmet(), a.pos));
    Destroy();
  }

  internal override string GetStats() => "Gives you the Well Fed buff.\nMakes a nice helmet after you eat it.";
}

[System.Serializable]
[ObjectInfo("pumpkin", "Yummy")]
class WellFedStatus : StackingStatus, IBaseActionModifier, IAttackDamageModifier {
  public WellFedStatus(int stacks) : base(stacks) {}

  public BaseAction Modify(BaseAction input) {
    stacks--;
    return input;
  }

  public int Modify(int input) {
    return input + 1;
  }

  public override string Info() => $"Deal +1 damage!";
}