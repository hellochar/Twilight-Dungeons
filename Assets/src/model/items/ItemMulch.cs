using System;

[Serializable]
[ObjectInfo("mulch", description: "Your item has decomposed and become mulch!")]
public class ItemMulch : Item, IStackable {
  public ItemMulch(int stacks) {
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
}
