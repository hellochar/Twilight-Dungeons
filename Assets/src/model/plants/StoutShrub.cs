using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// themes - power in numbers, "fight back", grows back, persistent, fast growers
// not elegant but it gets it done
// getting many stacks of an item
[Serializable]
public class StoutShrub : Plant {
  public static int waterCost => 70;
  [Serializable]
  class Mature : MaturePlantStage {
    public readonly bool growChild;

    public Mature(bool growChild = true) {
      this.growChild = growChild;
    }

    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      if (growChild) {
        plant.floor?.Put(new StoutShrub(plant.pos, new Mature(false)));
        harvestOptions.Add(new Inventory(
          new ItemSeed(typeof(StoutShrub), 2)
        ));
      }
      harvestOptions.Add(new Inventory(new ItemThicket(MyRandom.Range(6, 10))));
      harvestOptions.Add(new Inventory(new ItemPrickler(MyRandom.Range(4, 7))));
      harvestOptions.Add(new Inventory(new ItemHeartyVeggie()));
    }
  }

  public StoutShrub(Vector2Int pos) : base(pos, new Seed(3)) {
    stage.NextStage = new Mature();
  }

  private StoutShrub(Vector2Int pos, PlantStage stage) : base(pos, stage) {}

  protected override void HandleEnterFloor() {
    if (stage is Mature m && m.growChild) {
      floor.Put(new StoutShrub(pos, new Mature(false)));
    }
  }
}

[Serializable]
[ObjectInfo("thicket")]
public class ItemThicket : EquippableItem, IStackable, IBodyTakeAttackDamageHandler {
  internal override string GetStats() => "Constrict enemies who attack you for 6 turns.";
  public override EquipmentSlot slot => EquipmentSlot.Armor;
  private int _stacks;
  public int stacks {
    get => _stacks;
    set {
      if (value < 0) {
        throw new ArgumentException("Setting negative stack!" + this + " to " + value);
      }
      _stacks = value;
      if (_stacks == 0) {
        Destroy();
      }
    }
  }
  public int stacksMax => 100;
  public ItemThicket(int stacks) {
    this.stacks = stacks;
  }

  public void HandleTakeAttackDamage(int damage, int hp, Actor source) {
    if (source.faction != Faction.Ally) {
      source.statuses.Add(new ConstrictedStatus(null, 6));
      this.stacks--;
    }
  }
}

[Serializable]
[ObjectInfo("prickler")]
public class ItemPrickler : EquippableItem, IWeapon, IStackable, IAttackHandler {
  internal override string GetStats() => "Leaves a Prickly Growth on the attacked Creature's tile, which deals 3 attack damage to the Creature standing over it next turn.";
  private int _stacks;

  public int stacks {
    get => _stacks;
    set {
      if (value < 0) {
        throw new ArgumentException("Setting negative stack!" + this + " to " + value);
      }
      _stacks = value;
      if (_stacks == 0) {
        Destroy();
      }
    }
  }

  public int stacksMax => 100;
  public (int, int) AttackSpread => (1, 2);
  public override EquipmentSlot slot => EquipmentSlot.Weapon;

  public ItemPrickler(int stacks) {
    this.stacks = stacks;
  }

  public ItemPrickler() : this(1) { }

  public void OnAttack(int damage, Body target) {
    if (target is Actor) {
      target.floor.Put(new PricklyGrowth(target.pos));
    }
  }
}

[Serializable]
[ObjectInfo("prickly-growth", description: "Next turn, deal 3 attack damage to the Creature standing over the Prickly Growth.")]
class PricklyGrowth : Grass, ISteppable {
  public PricklyGrowth(Vector2Int pos) : base(pos) {
    timeNextAction = GameModel.main.time + 1;
  }
  public float timeNextAction { get; set; }
  public float turnPriority => 11;
  public float Step() {
    OnNoteworthyAction();
    body?.TakeAttackDamage(3, GameModel.main.player);
    KillSelf();
    return 3;
  }
}

[Serializable]
[ObjectInfo("stout-shield", "")]
public class ItemStoutShield : EquippableItem, IDurable, IAttackDamageTakenModifier {
  // 50% chance per turn
  private static float prdC50 = (float) PseudoRandomDistribution.CfromP(0.5m);
  public override EquipmentSlot slot => EquipmentSlot.Offhand;
  internal override string GetStats() => "50% chance to block all damage from an attack.";

  public int durability { get; set; }
  public int maxDurability { get; protected set; }
  private PseudoRandomDistribution prd;

  public ItemStoutShield() {
    this.maxDurability = 20;
    this.durability = maxDurability;
    prd = new PseudoRandomDistribution(prdC50);
  }

  public int Modify(int damage) {
    // invert the test so the very first turn is actually 70%, but successive blocks are unlikely
    if (damage > 0 && !(prd.Test())) {
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
  internal override string GetStats() => "Heal 4 HP over 100 turns.\nWill not tick down if you're at full HP.";
  public ItemHeartyVeggie() {}

  public void Eat(Actor a) {
    a.statuses.Add(new HeartyVeggieStatus(4));
    Destroy();
  }
}

[Serializable]
[ObjectInfo("hearty-veggie")]
class HeartyVeggieStatus : StackingStatus, IActionPerformedHandler {
  int turnsLeft = 25;

  public HeartyVeggieStatus(int stacks) : base(stacks) { }

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (actor.hp < actor.maxHp) {
      turnsLeft--;
      if (turnsLeft == 0) {
        turnsLeft = 25;
        actor.Heal(1);
        stacks--;
      }
    }
  }

  public override string Info() => $"Heal {stacks} more HP over {stacks*25} turns. Next tick in {turnsLeft} turns (paused while at full HP).";
}
