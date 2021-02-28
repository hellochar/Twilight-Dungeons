using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class ChangErsWillow : Plant {
  public override string displayName => "Chang-Er's Willow";
  [Serializable]
  class Mature : PlantStage {
    public override float StepTime => 99999;
    public override void Step() { }

    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(ChangErsWillow)),
        new ItemSeed(typeof(ChangErsWillow)),
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
class ItemFlowerBuds : Item, IDurable, IEdible {
  public int durability { get; set; }

  public int maxDurability => 4;

  public ItemFlowerBuds() {
    durability = maxDurability;
  }

  public void Eat(Actor a) {
    a.Heal(1);
    a.statuses.Add(new StrengthStatus(3));
    this.ReduceDurability();
  }

  internal override string GetStats() => "Eat to heal 1 HP and get 3 stacks of Strength.";
}

[Serializable]
[ObjectInfo("catkin", "Thousands of tiny flowers packaged in a single stem. Legend says wearing a willow catkin will gift you the Moon Goddess' protection.")]
internal class ItemCatkin : EquippableItem, IDurable, ITakeAnyDamageHandler {
  internal override string GetStats() => "When you take damage from any source, get the Recovering Status, which heals an equivalent amount after 50 turns.";
  public override EquipmentSlot slot => EquipmentSlot.Headwear;

  public int durability { get; set; }

  public int maxDurability => 5;

  public ItemCatkin() {
    durability = maxDurability;
  }

  public void HandleTakeAnyDamage(int damage) {
    if (damage > 0) {
      player.statuses.Add(new RecoveringStatus(damage));
      this.ReduceDurability();
    }
  }
}

[ObjectInfo("recovering")]
public class RecoveringStatus : StackingStatus {
  public override StackingMode stackingMode => StackingMode.Independent;
  int turnsLeft = 50;
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
internal class ItemHardenedSap : EquippableItem, IDurable, IHealHandler, IMaxHPModifier {
  public override EquipmentSlot slot => EquipmentSlot.Armor;

  public int durability { get; set; }

  public int maxDurability => 8;

  public ItemHardenedSap() {
    durability = maxDurability;
  }

  internal override string GetStats() => "+4 Max HP.\nWhen you heal, also gain that many stacks of the Armored Status.";

  public void HandleHeal(int amount) {
    if (amount > 0) {
      player.statuses.Add(new ArmoredStatus(amount));
      this.ReduceDurability();
    }
  }

  public int Modify(int input) {
    return input + 4;
  }
}

[Serializable]
[ObjectInfo("crescent-vengeance", "Chang-Er's disciples spent many years crafting an item worthy of their Goddess's attention. They succeeded.")]
internal class ItemCrescentVengeance : EquippableItem, IWeapon, IDurable {
  internal override string GetStats() => "When possible, Crescent Vengeance removes a stack of the Armored Status rather than lose durability.";
  public override EquipmentSlot slot => EquipmentSlot.Weapon;
  private int m_durability;
  public int durability {
    get => m_durability;
    set {
      if (value >= m_durability) {
        m_durability = value;
        return;
      }
      var status = player?.statuses.FindOfType<ArmoredStatus>();
      if (status == null) {
        m_durability = value;
        return;
      }

      var amountToLose = m_durability - value;
      var stacksLost = Mathf.Max(amountToLose, status.stacks);
      status.stacks -= stacksLost;
      amountToLose -= stacksLost;

      m_durability -= amountToLose;
    }
  }

  public int maxDurability => 15;

  public (int, int) AttackSpread => (3, 5);

  public ItemCrescentVengeance() {
    durability = maxDurability;
  }
}