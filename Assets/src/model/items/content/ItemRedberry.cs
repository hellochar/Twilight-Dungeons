using System;

[Serializable]
[ObjectInfo(spriteName: "redberry", flavorText: "Small but packed with goodness!")]
class ItemRedberry : Item, IUsable {
  public ItemRedberry(int stacks) : base(stacks) {
  }

  public ItemRedberry() : this(3) { }

  public override int stacksMax => 10;

  public void Use(Actor a) {
    a.Heal(2);
    stacks--;
  }

  internal override string GetStats() => "Heals 2 HP.";
}
