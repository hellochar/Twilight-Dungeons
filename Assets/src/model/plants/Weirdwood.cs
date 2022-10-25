using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class Weirdwood : Plant {
  [Serializable]
  class Mature : PlantStage {
    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Weirdwood), 2),
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
  }

  public Weirdwood(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Mature();
  }
}

[Serializable]
[ObjectInfo("vile-potion", "Weirdwood roots are notoriously used in demonic rituals... You wonder why?")]
internal class ItemVilePotion : Item, IUsable {
  public ItemVilePotion(int stacks = 3) : base(stacks) {}
  public override int stacksMax => 3;

  public void Use(Actor actor) {
    var player = (Player) actor;
    var floor = actor.floor;
    var enemies = player.GetVisibleActors(Faction.Enemy);
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

  internal override string GetStats() => "Spawns Vile Growths in lines towards every visible enemy.\nVile Growth does 1 damage per turn to any creature standing over it. Lasts 12 turns.";
}

[Serializable]
[ObjectInfo("vile-growth", description: "Deals 1 damage per turn to any creature standing over it.", flavorText: "Toxic tentacles erupt from the floor!")]
internal class VileGrowth : Grass, ISteppable, INoTurnDelay {
  int turns = 12;
  public VileGrowth(Vector2Int pos) : base(pos) {
    timeNextAction = timeCreated;
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
  public float turnPriority => 14;
}

[Serializable]
[ObjectInfo("colored_transparent_packed_39", "")]
internal class ItemBackstepShoes : EquippableItem, IAttackHandler {
  internal override string GetStats() => "After you make an attack, get 3 Free Moves.";

  public void OnAttack(int damage, Body target) {
    if (player.statuses.FindOfType<FreeMoveStatus>() == null) {
      player.statuses.Add(new FreeMoveStatus(3));
      stacks--;
    }
  }

  public override EquipmentSlot slot => EquipmentSlot.Footwear;

  public override int stacksMax => 7;
  public override bool disjoint => true;
}

[Serializable]
[ObjectInfo("witchs-shiv", "Wendy the Witch,\nFound the Snitch,\nNow he's lying,\nIn a ditch")]
internal class ItemWitchsShiv : EquippableItem, IWeapon, IAttackHandler {
  public override string displayName => "Witch's Shiv";
  public override EquipmentSlot slot => EquipmentSlot.Weapon;
  internal override string GetStats() => "Attacking a target fears them for 10 turns.";

  public (int, int) AttackSpread => (2, 2);

  public override int stacksMax => 3;
  public override bool disjoint => true;

  public void OnAttack(int damage, Body target) {
    if (target is Actor actor && !(actor.task is RunAwayTask) && !(actor is Boss)) {
      actor.SetTasks(new RunAwayTask(actor, player.pos, 10));
    }
  }
}