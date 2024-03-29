using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class Wildwood : Plant {
  [Serializable]
  class Mature : MaturePlantStage {
    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Wildwood), 2),
        new ItemStick()
      ));
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Wildwood)),
        new ItemWildwoodLeaf(3),
        new ItemWildwoodWreath()
      ));
      harvestOptions.Add(new Inventory(
        new ItemWildwoodRod()
      ));
    }
  }

  public Wildwood(Vector2Int pos) : base(pos) {
    stage.NextStage = new Mature();
  }
}

[Serializable]
[ObjectInfo("colored_transparent_packed_179", "It seems to bend and twist on its own, as if it were wielding you!")]
internal class ItemWildwoodRod : EquippableItem, IWeapon, IDurable, IActionPerformedHandler {
  public override EquipmentSlot slot => EquipmentSlot.Weapon;

  public ItemWildwoodRod() {
    durability = maxDurability;
  }

  public (int, int) AttackSpread => (3, 5);

  public int durability { get; set; }

  public int maxDurability => 20;

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