using System;

[Serializable]
[ObjectInfo("mulch", description: "Your item has decomposed and become mulch!")]
public class ItemMulch : Item {
  public ItemMulch(int stacks) : base(stacks) {
  }
  public override int stacksMax => 100;
}
