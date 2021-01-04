using System.Linq;
using UnityEngine;

public class Wildwood : Plant {
  public override int maxWater => 5;
  class Mature : PlantStage {
    public override float StepTime => 99999;
    public override void Step() { }
    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Wildwood)),
        new ItemSeed(typeof(Wildwood)),
        new ItemStick()
      ));
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Wildwood)),
        new ItemWildwoodLeaf(3),
        new ItemWildwoodWreath()
      ));
      harvestOptions.Add(new Inventory(
        new ItemWildwoodLeaf(3),
        new ItemWildwoodRod()
      ));
    }
    public override string getUIText() => $"Ready to harvest.";
  }

  public Wildwood(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Mature();
  }
}

[ObjectInfo("colored_transparent_packed_179", "It seems to bend and twist on its own, as if it were wielding you!")]
internal class ItemWildwoodRod : EquippableItem, IWeapon, IDurable, IActionPerformedHandler {
  public override EquipmentSlot slot => EquipmentSlot.Weapon;

  public ItemWildwoodRod() {
    durability = maxDurability;
  }

  public (int, int) AttackSpread => (3, 5);

  public int durability { get; set; }

  public int maxDurability => 30;

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