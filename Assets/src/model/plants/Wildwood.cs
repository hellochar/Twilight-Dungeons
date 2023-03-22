using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Wildwood : Plant {
  [Serializable]
  class Mature : PlantStage {
    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Wildwood), 2),
        new ItemStick(7)
      ));
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Wildwood)),
        new ItemWildwoodLeaf(3),
        new ItemWildwoodWreath(10)
      ));
      harvestOptions.Add(new Inventory(
        new ItemWildwoodRod(10)
      ));
    }
  }

  public Wildwood(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Mature();
  }
}

[Serializable]
[ObjectInfo("colored_transparent_packed_179", "It seems to bend and twist on its own, as if it were wielding you!")]
internal class ItemWildwoodRod : EquippableItem, IWeapon, IActionPerformedHandler {
  public static int yieldCost = 6;

  public ItemWildwoodRod(int stacks) : base(stacks) {}

  public override EquipmentSlot slot => EquipmentSlot.Weapon;

  public (int, int) AttackSpread => (3, 5);

  public override int stacksMax => int.MaxValue;
  // public override bool disjoint => true;

  internal override string GetStats() => "Automatically attack an adjacent enemy when you move.";

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (final.Type == ActionType.MOVE) {
      var target = player.floor.AdjacentActors(player.pos).Where(a => a.faction == Faction.Enemy).FirstOrDefault();
      if (target != null) {
        player.Perform(new AttackBaseAction(player, target));
      }
    }
  }
}