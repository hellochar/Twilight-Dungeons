using System;
using System.Linq;
using UnityEngine;

public class Weirdwood : Plant {
  public override int maxWater => 5;
  class Mature : PlantStage {
    public override float StepTime => 99999;
    public override void Step() { }
    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Weirdwood)),
        new ItemSeed(typeof(Weirdwood)),
        new ItemStick()
      ));
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Weirdwood)),
        new ItemWitchsShiv(),
        new ItemBackstepShoes()
      ));
      harvestOptions.Add(new Inventory(new ItemVilePotion()));
      // harvestOptions.Add(new Inventory(
      //   new ItemWildwoodLeaf(3),
      //   new ItemWildwoodRod()
      // ));
    }
    public override string getUIText() => $"Ready to harvest.";
  }

  public Weirdwood(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Mature();
  }
}

[ObjectInfo("vile-potion", "Weirdwood roots are notorious for their use in demonic rituals... You wonder why?")]
internal class ItemVilePotion : Item, IUsable {
  public ItemVilePotion() {}

  public void Use(Actor actor) {
    var floor = actor.floor;
    var enemies = floor.ActorsInCircle(actor.pos, 5).Where((a) => a.faction == Faction.Enemy);
    var start = actor.pos;
    foreach (var enemy in enemies) {
      foreach (var pos in floor.EnumerateLine(start, enemy.pos).Where((pos) => floor.tiles[pos] is Ground)) {
        floor.Put(new VileGrowth(pos));
      }
    }
  }

  internal override string GetStats() => "Spawns Vile Growths in a line towards enemies in range 5.\nVile Growth does 1 damage per turn to the creature standing over it. Lasts 12 turns.";
}

internal class VileGrowth : Grass {
  int turns = 12;
  public VileGrowth(Vector2Int pos) : base(pos) {
    timeNextAction = timeCreated + 1;
  }

  protected override float Step() {
    actor?.TakeDamage(1);
    if (--turns <= 0) {
      Kill();
    }
    return 1;
  }
}

[ObjectInfo("colored_transparent_packed_39", "")]
internal class ItemBackstepShoes : EquippableItem, IDurable, IAttackHandler {
  internal override string GetStats() => "After you make an attack, get a free movement.";

  public void OnAttack(Actor target) {
    player.statuses.Add(new FreeMoveStatus());
  }

  public ItemBackstepShoes() {
    durability = maxDurability;
  }

  public override EquipmentSlot slot => EquipmentSlot.Feet;

  public int durability { get; set; }

  public int maxDurability => 20;
}

[ObjectInfo("colored_transparent_packed_321", "Wendy the Witch,\nFound the Snitch,\nNow he's lying,\nIn a ditch")]
internal class ItemWitchsShiv : EquippableItem, IWeapon, IDurable, IAttackHandler {
  public override string displayName => "Witch's Shiv";
  public override EquipmentSlot slot => EquipmentSlot.Weapon;
  internal override string GetStats() => "Attacking a target fears them for 3 turns.";

  public ItemWitchsShiv() {
    durability = maxDurability;
  }

  public (int, int) AttackSpread => (1, 3);

  public int durability { get; set; }

  public int maxDurability => 12;

  public void OnAttack(Actor target) {
    target.SetTasks(new RunAwayTask(target, player.pos, 3));
  }
}