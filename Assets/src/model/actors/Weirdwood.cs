using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class Weirdwood : Plant {
  [Serializable]
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

[Serializable]
[ObjectInfo("vile-potion", "Weirdwood roots are notorious for their use in demonic rituals... You wonder why?")]
internal class ItemVilePotion : Item, IStackable, IUsable {
  public ItemVilePotion(int stacks = 4) {
    this.stacks = stacks;
  }
  public int stacksMax => 4;

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


  public void Use(Actor actor) {
    var floor = actor.floor;
    var enemies = floor.ActorsInCircle(actor.pos, 6).Where((a) => a.faction == Faction.Enemy);
    var start = actor.pos;
    if (enemies.Any()) {
      foreach (var enemy in enemies) {
        foreach (var pos in floor.EnumerateLine(start, enemy.pos).Where((pos) => floor.tiles[pos] is Ground)) {
          floor.Put(new VileGrowth(pos));
        }
      }
      stacks--;
    } else {
      // throw new CannotPerformActionException("No enemies around.");
    }
  }

  internal override string GetStats() => "Spawns Vile Growths in a line towards enemies in range 5.\nVile Growth does 1 damage per turn to the creature standing over it. Lasts 12 turns.";
}

[Serializable]
internal class VileGrowth : Grass, ISteppable {
  int turns = 12;
  public VileGrowth(Vector2Int pos) : base(pos) {
    timeNextAction = timeCreated + 1;
  }

  public float Step() {
    actor?.TakeDamage(1, GameModel.main.player);
    OnNoteworthyAction();
    if (--turns <= 0) {
      Kill(this);
    }
    return 1;
  }

  public float timeNextAction { get; set; }
  public float turnPriority => 50;
}

[Serializable]
[ObjectInfo("colored_transparent_packed_39", "")]
internal class ItemBackstepShoes : EquippableItem, IDurable, IAttackHandler {
  internal override string GetStats() => "After you make an attack, get a free movement.";

  public void OnAttack(int damage, Body target) {
    player.statuses.Add(new FreeMoveStatus());
    this.ReduceDurability();
  }

  public ItemBackstepShoes() {
    durability = maxDurability;
  }

  public override EquipmentSlot slot => EquipmentSlot.Feet;

  public int durability { get; set; }

  public int maxDurability => 10;
}

[Serializable]
[ObjectInfo("witchs-shiv", "Wendy the Witch,\nFound the Snitch,\nNow he's lying,\nIn a ditch")]
internal class ItemWitchsShiv : EquippableItem, IWeapon, IDurable, IAttackHandler {
  public override string displayName => "Witch's Shiv";
  public override EquipmentSlot slot => EquipmentSlot.Weapon;
  internal override string GetStats() => "Attacking a target fears them for 3 turns.";

  public ItemWitchsShiv() {
    durability = maxDurability;
  }

  public (int, int) AttackSpread => (1, 3);

  public int durability { get; set; }

  public int maxDurability => 5;

  public void OnAttack(int damage, Body target) {
    if (target is Actor actor) {
      actor.SetTasks(new RunAwayTask(actor, player.pos, 3));
    }
  }
}