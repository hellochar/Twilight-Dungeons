using System;
using System.Collections.Generic;
using UnityEngine;

public class Thornleaf : Plant {
  public override int maxWater => 5;

  class Mature : PlantStage {
    public override float StepTime => 99999;
    public override void Step() { }
    public override string getUIText() => $"Ready to harvest.";
  }

  public Thornleaf(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Sapling();
    stage.NextStage.NextStage = new Mature();
  }


  public override Inventory CullRewards() {
    return new Inventory(new ItemSeed(typeof(Thornleaf)));
  }

  public override Inventory HarvestRewards() {
    if (stage is Mature) {
      return new Inventory(
        new ItemThornShield(),
        new ItemThornmail()
      );
    }
    return null;
  }

  public override void Harvest() {
    base.Harvest();
    stage = new Sapling();
    stage.NextStage = new Mature();
  }
}

[ObjectInfo("thornmail", "Spiky!")]
internal class ItemThornmail : EquippableItem, IDurable, IMaxHPModifier {
  public override EquipmentSlot slot => EquipmentSlot.Body;

  public int durability { get; set; }

  public int maxDurability => 20;

  public ItemThornmail() {
    durability = maxDurability;
    OnEquipped += HandleEquipped;
    OnUnequipped += HandleUnequipped;
  }

  private void HandleEquipped(Player p) {
    p.OnTakeAttackDamage += HandleTakeAttackDamage;
  }

  private void HandleUnequipped(Player p) {
    p.OnTakeAttackDamage -= HandleTakeAttackDamage;
  }

  private void HandleTakeAttackDamage(int dmg, int hp, Actor source) {
    var player = GameModel.main.player;
    if (source != player) {
      source.TakeDamage(1);
      this.ReduceDurability();
    }
  }

  public int Modify(int input) {
    return input + 4;
  }

  internal override string GetStats() => "Max HP +4.\nReturn 1 damage when an enemy hits you.";
}

[ObjectInfo("thornshield", "Also spiky!")]
internal class ItemThornShield : EquippableItem, IDurable, IModifierProvider {
  private class TakeLessDamage : IAttackDamageTakenModifier {
    private ItemThornShield itemThornShield;

    public TakeLessDamage(ItemThornShield itemThornShield) {
      this.itemThornShield = itemThornShield;
    }

    public int Modify(int input) {
      itemThornShield.ReduceDurability();
      return input - 1;
    }
  }
  private class DealMoreDamage : IAttackDamageModifier {
    private ItemThornShield itemThornShield;

    public DealMoreDamage(ItemThornShield itemThornShield) {
      this.itemThornShield = itemThornShield;
    }

    public int Modify(int input) {
      itemThornShield.ReduceDurability();
      return input + 1;
    }
  }

  private List<object> Modifiers;

  public IEnumerable<object> MyModifiers => Modifiers;

  public override EquipmentSlot slot => EquipmentSlot.Shield;

  public int durability { get; set; }

  public int maxDurability => 40;

  public ItemThornShield() {
    durability = maxDurability;
    Modifiers = new List<object> { new TakeLessDamage(this), new DealMoreDamage(this) };
  }

  internal override string GetStats() => "Blocks 1 damage.\nDeal 1 more attack damage.\nBoth attacking and defending use durability.";
}