using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[Serializable]
/// Frizzlefens - ideas are:
/// stoic, meager, minimal. A few decisive choices that make big impacts.
/// Quality over quantity.
/// Thick branch - 3-5 damage, 5 durability, attacks take 1.5 turns
/// Plated armor - blocks 6 damage, then 5, then 4, then 3, then 2, then 1
/// Barkmeal - Eat to obtain a buff that heals 4 and gives 4 max HP for 100 turns.
/// Stompin Boots - when you walk onto any Grass, kill it. Gain a status that blocks the next 1 damage.
public class Frizzlefen : Plant {
  [Serializable]
  class Mature : PlantStage {
    public override float StepTime => 99999;
    public override void Step() { }
    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Frizzlefen)),
        new ItemThickBranch(),
        new ItemThickBranch()
      ));
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Frizzlefen)),
        new ItemBarkmeal(),
        new ItemPlatedArmor()
      ));
      harvestOptions.Add(new Inventory(
        new ItemStompinBoots()
      ));
    }
  }

  public Frizzlefen(Vector2Int pos) : base(pos, new Seed(400)) {
    stage.NextStage = new Mature();
  }
}

[Serializable]
[ObjectInfo("thick-stick")]
class ItemThickBranch : EquippableItem, IWeapon, IDurable, IActionCostModifier {
  public override EquipmentSlot slot => EquipmentSlot.Weapon;

  public (int, int) AttackSpread => (3, 5);

  public int durability { get; set; }

  public int maxDurability => 5;

  public ItemThickBranch() {
    durability = maxDurability;
  }

  public ActionCosts Modify(ActionCosts input) {
    input[ActionType.ATTACK] *= 1.5f;
    return input;
  }

  internal override string GetStats() => "Attacks 50% slower.";
}

[Serializable]
[ObjectInfo("plated-armor")]
class ItemPlatedArmor : EquippableItem, IDurable, IAttackDamageTakenModifier {
  public override EquipmentSlot slot => EquipmentSlot.Armor;

  public int durability { get; set; }
  int damageBlock => (maxDurability + 1) - durability;
  public int maxDurability => 6;

  public ItemPlatedArmor() {
    durability = maxDurability;
  }

  public int Modify(int damage) {
    if (damage > 0) {
      this.ReduceDurability();
      return damage - damageBlock;
    } else {
      return damage;
    }
  }

  internal override string GetStats() => $"Blocks {damageBlock} damage. Each time this blocks damage, increase damage blocked by 1.";
}

[Serializable]
[ObjectInfo("colored_transparent_packed_675")]
class ItemBarkmeal : Item, IEdible {
  public void Eat(Actor a) {
    a.statuses.Add(new BarkmealStatus());
    Destroy();
  }

  internal override string GetStats() => "Eat to permanently gain +4 max HP.";
}

[Serializable]
[ObjectInfo("colored_transparent_packed_675")]
class BarkmealStatus : StackingStatus, IMaxHPModifier {
  public override StackingMode stackingMode => StackingMode.Add;
  public BarkmealStatus() : base(4) {}

  public override string Info() => $"+{stacks} max HP.";

  public int Modify(int input) {
    return input + stacks;
  }
}

[Serializable]
[ObjectInfo("stompinboots", "A thick crust and good sole can protect your feet for years to come.")]
class ItemStompinBoots : EquippableItem, IBodyMoveHandler {
  public override EquipmentSlot slot => EquipmentSlot.Footwear;

  public int durability { get; set; }

  public int maxDurability => 45;

  public void HandleMove(Vector2Int newPos, Vector2Int oldPos) {
    if (player.grass != null) {
      player.grass.Kill(player);
      player.statuses.Add(new ArmoredStatus());
    }
  }

  internal override string GetStats() => "Everlasting.\nWhen you walk on to any Grass, kill it and gain 1 stack of Armored.";
}

[Serializable]
[ObjectInfo("colored_transparent_packed_228")]
class ArmoredStatus : StackingStatus, IAttackDamageTakenModifier {
  public override StackingMode stackingMode => StackingMode.Add;
  public override string Info() => $"Block 1 damage from the next {stacks} attacks!";

  public int Modify(int input) {
    if (input > 0) {
      stacks--;
      return input - 1;
    }
    return input;
  }
}