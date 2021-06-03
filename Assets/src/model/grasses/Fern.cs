using System;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(spriteName: "fern", description: "Blocks vision, but creatures can still walk over it. You can cut it down when standing over it.")]
public class Fern : Grass, IBlocksVision {
  public static bool CanOccupy(Tile tile) => tile is Ground && (tile.body == null || tile.body is Actor);

  public Fern(Vector2Int pos) : base(pos) {}

  public void CutDown(Player player) {
    Kill(player);
    // BecomeItemInInventory(new ItemFern(1), player);
  }
}

// [System.Serializable]
// [ObjectInfo("fern")]
// public class ItemFern : Item, IStackable {
//   internal override string GetStats() => "Collect 50 ferns ";

//   public ItemFern(int stacks) {
//     this.stacks = stacks;
//   }
//   public int stacksMax => 100;

//   private int _stacks;
//   public int stacks {
//     get => _stacks;
//     set {
//       if (value < 0) {
//         throw new ArgumentException("Setting negative stack!" + this + " to " + value);
//       }
//       _stacks = value;
//       if (_stacks == 0) {
//         Destroy();
//       }
//     }
//   }
// }