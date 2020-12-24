using System.Linq;
using UnityEngine;

public class BerryBush : Plant {
  public override int maxWater => 4;
  class Mature : PlantStage {
    public int numBerries = 3;

    public override float StepTime => 250;

    public override void Step() {
      numBerries += 3;
    }

    public override string getUIText() => $"Contains {numBerries} berries.\n\nGrows 3 berries in {plant.timeNextAction - plant.age} turns.";
  }

  public BerryBush(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Young();
    stage.NextStage.NextStage = new Mature();
  }

  public override Inventory HarvestRewards() {
    if (stage is Mature mature) {
      var stacks = mature.numBerries;
      if (stacks > 0) {
        var wantedStacks = stacks;
        mature.numBerries = 0;
        return new Inventory(new ItemBerries(wantedStacks));
      }
    }
    return null;
  }

  public override Inventory CullRewards() {
    if (stage is Mature) {
      return new Inventory(new ItemSeed(typeof(BerryBush)), new ItemSeed(typeof(BerryBush)));
    } else {
      return new Inventory(new ItemSeed(typeof(BerryBush)));
    }
  }
}

public class ItemPumpkin : Item, IEdible {
  public ItemPumpkin() {
  }

  public void Eat(Actor a) {
    if (a is Player p) {
      p.IncreaseFullness(0.2f);
      var helmet = new ItemPumpkinHelmet();
      if (!p.inventory.AddItem(helmet, a)) {
        var itemOnGround = new ItemOnGround(a.pos, helmet);
      }
    }
    Destroy();
  }

  internal override string GetStats() => "Restores 20% food!";
}

[ObjectInfo(spriteName: "pumpkin-helmet", flavorText: "It doesn't smell great but it protects your noggin.")]
internal class ItemPumpkinHelmet : EquippableItem, IDurable, IDamageTakenModifier, IActionCostModifier {
  public override EquipmentSlot slot => EquipmentSlot.Head;
  public int durability { get; set; }
  public int maxDurability { get; protected set; }

  public ItemPumpkinHelmet() {
    this.maxDurability = 30;
    this.durability = maxDurability;
  }

  public int Modify(int damage) {
    this.ReduceDurability();
    return damage - 1;
  }

  public ActionCosts Modify(ActionCosts input) {
    input[ActionType.ATTACK] *= 1.5f;
    return input;
  }

  internal override string GetStats() => "Reduces damage taken by 1.\nYou attack 50% slower.";
}