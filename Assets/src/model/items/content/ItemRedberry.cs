using System;

[ObjectInfo(spriteName: "redberry", flavorText: "Small but packed with goodness!")]
class ItemRedberry : Item, IStackable, IUsable {
  public ItemRedberry(int stacks) {
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

  public void Use(Actor a) {
    a.Heal(3);
    if (a is Player p) {
      p.IncreaseFullness(0.05f);
    }
    stacks--;
  }

  internal override string GetStats() => "Heals 3 HP.";
}
