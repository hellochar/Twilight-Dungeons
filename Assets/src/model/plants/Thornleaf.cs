using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Thornleaf : Plant {

  [Serializable]
  class Mature : MaturePlantStage {
    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Thornleaf), 2),
        new ItemStick()
      ));
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Thornleaf)),
        new ItemThornShield(),
        new ItemCrownOfThorns()
      ));
      harvestOptions.Add(new Inventory(
        new ItemBlademail()
      ));
    }
  }

  public Thornleaf(Vector2Int pos) : base(pos) {
    stage.NextStage = new Mature();
  }
}

[Serializable]
[ObjectInfo("crown-of-thorns", "Layers of spiky leaves protrude in all directions.")]
internal class ItemCrownOfThorns : EquippableItem, IDurable, IBodyTakeAttackDamageHandler {
  public override EquipmentSlot slot => EquipmentSlot.Headwear;
  public override string displayName => "Crown of Thorns";

  public int durability { get; set; }

  public int maxDurability => 14;

  public ItemCrownOfThorns() {
    durability = maxDurability;
  }

  public void HandleTakeAttackDamage(int dmg, int hp, Actor source) {
    var player = GameModel.main.player;
    // rarely, the thing that attacks you may already be dead (AKA parasite)
    if (source != player && !source.IsDead) {
      source.TakeDamage(2, player);
      this.ReduceDurability();
    }
  }

  internal override string GetStats() => "When an enemy attacks you, deal 2 damage back to the attacker.";
}

[Serializable]
[ObjectInfo("thornshield", "Thornleaf timber is actually quite supple, allowing you to get a good bash in once in a while.")]
internal class ItemThornShield : EquippableItem, IDurable, IModifierProvider {
  [Serializable]
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
  [Serializable]
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

  public override EquipmentSlot slot => EquipmentSlot.Offhand;

  public int durability { get; set; }

  public int maxDurability => 11;

  public ItemThornShield() {
    durability = maxDurability;
    Modifiers = new List<object> { new TakeLessDamage(this), new DealMoreDamage(this) };
  }

  internal override string GetStats() => "Blocks 1 damage.\nDeal 1 more attack damage.\nBoth attacking and blocking use durability.";
}

[Serializable]
[ObjectInfo("heart-of-thorns", "Espheus died when her son no longer recognized her; her heart grew cold, then hard, then sharp.")]
internal class ItemBlademail : EquippableItem, IDurable, IAnyDamageTakenModifier, ITakeAnyDamageHandler {
  internal override string GetStats() => "Take 2 less damage from all sources.\nWhen you would take damage from any source, grow, sharpen, or trigger Bladegrass on all adjacent tiles.";

  public override EquipmentSlot slot => EquipmentSlot.Armor;
  public int durability { get; set; }
  public int maxDurability => 29;

  public ItemBlademail() {
    durability = maxDurability;
  }

  public void HandleTakeAnyDamage(int dmg) {
    foreach (var tile in player.floor.GetAdjacentTiles(player.pos).Where(Bladegrass.CanOccupy)) {
      if (tile.grass is Bladegrass g) {
        if (g.isSharp) {
          if (g.actor != null) {
            g.HandleActorEnter(g.actor);
          }
        } else {
          g.Sharpen();
        }
      } else {
        player.floor.Put(new Bladegrass(tile.pos));
      }
    }
    this.ReduceDurability();
  }

  public int Modify(int input) {
    return input - 2;
  }
}