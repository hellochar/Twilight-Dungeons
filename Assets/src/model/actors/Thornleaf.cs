using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Thornleaf : Plant {

  [Serializable]
  class Mature : PlantStage {
    public override float StepTime => 99999;
    public override void Step() { }

    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Thornleaf)),
        new ItemSeed(typeof(Thornleaf)),
        new ItemStick()
      ));
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Thornleaf)),
        new ItemThornShield(),
        new ItemThornmail()
      ));
      harvestOptions.Add(new Inventory(
        new ItemHeartOfThorns()
      ));
    }
  }

  public Thornleaf(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Mature();
  }
}

[Serializable]
[ObjectInfo("thornmail", "Spiky!")]
internal class ItemThornmail : EquippableItem, IDurable, IMaxHPModifier, IBodyTakeAttackDamageHandler {
  public override EquipmentSlot slot => EquipmentSlot.Body;

  public int durability { get; set; }

  public int maxDurability => 20;

  public ItemThornmail() {
    durability = maxDurability;
  }

  public void HandleTakeAttackDamage(int dmg, int hp, Actor source) {
    var player = GameModel.main.player;
    if (source != player) {
      source.TakeDamage(1, player);
      this.ReduceDurability();
    }
  }

  public int Modify(int input) {
    return input + 4;
  }

  internal override string GetStats() => "Max HP +4.\nReturn 1 damage when an enemy hits you.";
}

[Serializable]
[ObjectInfo("thornshield", "Also spiky!")]
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

  public override EquipmentSlot slot => EquipmentSlot.Shield;

  public int durability { get; set; }

  public int maxDurability => 12;

  public ItemThornShield() {
    durability = maxDurability;
    Modifiers = new List<object> { new TakeLessDamage(this), new DealMoreDamage(this) };
  }

  internal override string GetStats() => "Blocks 1 damage.\nDeal 1 more attack damage.\nBoth attacking and defending use durability.";
}

[Serializable]
[ObjectInfo("heart-of-thorns", "Espheus died when her son no longer recognized her; her heart grew cold, then hard, then sharp.")]
internal class ItemHeartOfThorns : EquippableItem, IDurable, IAnyDamageTakenModifier, ITakeAnyDamageHandler {
  internal override string GetStats() => "Take 2 less damage from all sources.\nWhen you would take damage from any source, grow, sharpen, or trigger Bladegrass on all adjacent squares.";

  public override EquipmentSlot slot => EquipmentSlot.Head;
  public int durability { get; set; }
  public int maxDurability => 36;

  public ItemHeartOfThorns() {
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