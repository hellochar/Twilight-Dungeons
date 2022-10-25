using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class ChangErsWillow : Plant {
  public override string displayName => "Chang-Er's Willow";
  [Serializable]
  class Mature : PlantStage {
    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(ChangErsWillow), 2),
        new ItemFlowerBuds()
      ));
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(ChangErsWillow)),
        new ItemCatkin(),
        new ItemHardenedSap()
      ));
      harvestOptions.Add(new Inventory(
        new ItemCrescentVengeance()
      ));
    }
  }

  public ChangErsWillow(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Mature();
  }
}

[Serializable]
[ObjectInfo("flower-buds")]
class ItemFlowerBuds : Item, IEdible {
  public override int stacksMax => 3;
  public override bool disjoint => true;

  public void Eat(Actor a) {
    a.Heal(1);
    a.statuses.Add(new StrengthStatus(2));
    stacks--;
  }

  internal override string GetStats() => "Eat to heal 1 HP and get 2 stacks of Strength.";
}

[Serializable]
[ObjectInfo("catkin", "Tiny flowers packaged up in a single stem - legend says wearing one will grant you the Moon Goddess's protection.")]
internal class ItemCatkin : EquippableItem, ITakeAnyDamageHandler {
  internal override string GetStats() => "When you take damage, heal an equivalent amount after 25 turns.";
  public override EquipmentSlot slot => EquipmentSlot.Headwear;

  public override int stacksMax => 3;
  public override bool disjoint => true;

  public void HandleTakeAnyDamage(int damage) {
    if (damage > 0) {
      player.statuses.Add(new RecoveringStatus(damage));
      stacks--;
    }
  }
}

[Serializable]
[ObjectInfo("recovering")]
public class RecoveringStatus : StackingStatus {
  public override StackingMode stackingMode => StackingMode.Independent;
  int turnsLeft = 25;
  public RecoveringStatus(int stacks) : base(stacks) {
  }

  public override void Step() {
    turnsLeft--;
    if (turnsLeft <= 0) {
      Remove();
    }
  }

  public override void End() {
    actor.Heal(stacks);
  }

  public override string Info() => $"In {turnsLeft} turns, heal {stacks} HP.";
}

[Serializable]
[ObjectInfo("hardened-sap")]
internal class ItemHardenedSap : EquippableItem, IHealHandler, IMaxHPModifier {
  public override EquipmentSlot slot => EquipmentSlot.Armor;

  public override int stacksMax => 5;
  public override bool disjoint => true;

  internal override string GetStats() => "+4 Max HP.\nWhen you heal, gain that many stacks of the Armored Status.";

  public void HandleHeal(int amount) {
    if (amount > 0) {
      player.statuses.Add(new ArmoredStatus(amount));
      stacks--;
    }
  }

  public int Modify(int input) {
    return input + 4;
  }
}

[Serializable]
[ObjectInfo("crescent-vengeance", "Chang-Er's disciples spent many years crafting an item worthy of their Goddess's attention.")]
internal class ItemCrescentVengeance : EquippableItem, IWeapon {
  internal override string GetStats() => "If possible, Crescent Vengeance removes a stack of the Armored Status rather than lose durability.";
  public override EquipmentSlot slot => EquipmentSlot.Weapon;
  public override int stacks {
    get => base.stacks;
    set {
      if (value >= base.stacks) {
        base.stacks = value;
        return;
      }
      var status = player?.statuses.FindOfType<ArmoredStatus>();
      if (status == null) {
        base.stacks = value;
        return;
      }

      var amountToLose = base.stacks - value;
      var stacksLost = Mathf.Min(amountToLose, status.stacks);
      status.stacks -= stacksLost;
      amountToLose -= stacksLost;

      base.stacks -= amountToLose;
    }
  }

  public override int stacksMax => 10;
  public override bool disjoint => true;

  public (int, int) AttackSpread => (3, 5);
}