[System.Serializable]
[ObjectInfo("pumpkin", "Round and heavy and filled with raw calories.")]
public class ItemPumpkin : Item, IEdible {
  public ItemPumpkin() {}

  public void Eat(Actor a) {
    a.statuses.Add(new StrengthStatus(6));
    a.floor.Put(new ItemOnGround(a.pos, new ItemPumpkinHelmet(), a.pos));
    Destroy();
  }

  internal override string GetStats() => "Gives 6 stacks of Strength.\nMakes a nice helmet after you eat it.";
}

[System.Serializable]
[ObjectInfo("strength", "You're feeling good!")]
class StrengthStatus : StackingStatus, IAttackDamageModifier {
  public StrengthStatus(int stacks) : base(stacks) {}

  public int Modify(int input) {
    stacks--;
    return input + 1;
  }

  public override string Info() => $"Your next {stacks} attacks deal +1 damage!";
}