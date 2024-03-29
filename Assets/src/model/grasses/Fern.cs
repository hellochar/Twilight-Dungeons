using System;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(spriteName: "fern", description: "Blocks vision, but creatures can still walk over it. You can cut it down when standing over it.")]
public class Fern : Grass, IBlocksVision {
  public static bool CanOccupy(Tile tile) => tile is Ground && (tile.body == null || tile.body is Actor);

  public Fern(Vector2Int pos) : base(pos) {}

  public void CutSelfAndAdjacent(Player player) {
    foreach (var fern in floor.GetAdjacentTiles(pos).Select(t => t.grass).Where(g => g is Fern).ToList()) {
      fern.Kill(player);
    }
  }
}

[Serializable]
[ObjectInfo("golden-fern")]
public class GoldenFern : Fern, IDeathHandler {
  public GoldenFern(Vector2Int pos) : base(pos) {
  }

  public void HandleDeath(Entity source) {
    floor.Put(new ItemOnGround(pos, new ItemGoldenFern()));
    // drop a golden fern on the ground
  }
}

[Serializable]
[ObjectInfo("golden-fern", description: "Leaves a trail of Ferns as you walk. Lasts one floor. Cannot be unequipped.")]
class ItemGoldenFern : EquippableItem, IBodyMoveHandler, IFloorChangeHandler, ISticky {
  public override EquipmentSlot slot => EquipmentSlot.Headwear;

  public void HandleFloorChanged(Floor newFloor, Floor oldFloor) {
    Destroy();
  }

  public void HandleMove(Vector2Int newPos, Vector2Int oldPos) {
    if (!(player.floor.grasses[oldPos] is Fern)) {
      player.floor.Put(new Fern(oldPos));
    }
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