using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class StoutShrub : Plant {
  [Serializable]
  class Mature : PlantStage {
    public override float StepTime => 999999;
    public readonly bool growChild;

    public Mature(bool growChild = true) {
      this.growChild = growChild;
    }

    public override void Step() {}

    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      if (growChild) {
        plant.floor?.Put(new StoutShrub(plant.pos, new Mature(false)));
        harvestOptions.Add(new Inventory(
          new ItemSeed(typeof(StoutShrub), 3)
        ));
      }
      harvestOptions.Add(new Inventory(new ItemThicket()));
      harvestOptions.Add(new Inventory(new ItemStoutShield()));
      harvestOptions.Add(new Inventory(new ItemHeartyVeggie()));
    }
  }

  public StoutShrub(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Mature();
  }

  private StoutShrub(Vector2Int pos, PlantStage stage) : base(pos, stage) {}
}

[Serializable]
[ObjectInfo("thicket")]
public class ItemThicket : EquippableItem, IDurable, IBodyTakeAttackDamageHandler {
  internal override string GetStats() => "Constrict enemies who attack you for 6 turns.";
  public override EquipmentSlot slot => EquipmentSlot.Armor;
  public int durability { get; set; }
  public int maxDurability => 15;
  public ItemThicket() {
    this.durability = maxDurability;
  }

  public void HandleTakeAttackDamage(int damage, int hp, Actor source) {
    source.statuses.Add(new ConstrictedStatus(null, 6));
    this.ReduceDurability();
  }
}


[Serializable]
[ObjectInfo("stout-shield", "")]
public class ItemStoutShield : EquippableItem, IDurable, IAttackDamageTakenModifier {
  public override EquipmentSlot slot => EquipmentSlot.Offhand;
  internal override string GetStats() => "50% chance to block all damage from an attack.";

  public int durability { get; set; }
  public int maxDurability { get; protected set; }

  public ItemStoutShield() {
    this.maxDurability = 20;
    this.durability = maxDurability;
  }

  public int Modify(int damage) {
    if (damage > 0 && MyRandom.value < 0.5f) {
      this.ReduceDurability();
      return 0;
    } else {
      return damage;
    }
  }
}

[Serializable]
[ObjectInfo("hearty-veggie", "Your mum always said to eat a lot of these!")]
public class ItemHeartyVeggie : Item, IEdible {
  internal override string GetStats() => "Heal 6 HP over 300 turns.\nWill not tick down if you're at full HP.";
  public ItemHeartyVeggie() {}

  public void Eat(Actor a) {
    a.statuses.Add(new HeartyVeggieStatus(6));
    Destroy();
  }
}

[Serializable]
[ObjectInfo("hearty-veggie")]
class HeartyVeggieStatus : StackingStatus, IActionPerformedHandler {
  int turnsLeft = 50;

  public HeartyVeggieStatus(int stacks) : base(stacks) { }

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (actor.hp < actor.maxHp) {
      turnsLeft--;
      if (turnsLeft == 0) {
        turnsLeft = 50;
        actor.Heal(1);
        stacks--;
      }
    }
  }

  public override string Info() => $"Heal {stacks} more HP over {stacks*50} turns. Next tick in {turnsLeft} turns (paused while at full HP).";
}
