using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[Serializable]
public class Kingshroom : Plant {
  [Serializable]
  class Mature : PlantStage {
    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Kingshroom), 2),
        new ItemGerm()
      ));
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Kingshroom)),
        new ItemMushroomCap(),
        new ItemKingshroomPowder()
      ));
      harvestOptions.Add(new Inventory(
        new ItemLivingArmor()
      ));
    }
  }

  public Kingshroom(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Mature();
  }
}

[Serializable]
[ObjectInfo("living-armor", "")]
internal class ItemLivingArmor : EquippableItem, ISticky, IActionPerformedHandler, IAttackDamageTakenModifier {
  public override EquipmentSlot slot => EquipmentSlot.Armor;
  public override int stacksMax => 60;
  public override bool disjoint => true;

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (final.Type == ActionType.MOVE) {
      stacks--;
    }
  }

  public int Modify(int input) => input - 2;

  internal override string GetStats() => "Blocks 2 attack damage.\nMoving reduces durability.\nCannot be unequipped.";
}

[Serializable]
[ObjectInfo("mushroom-cap", "Spongy and porous holes dot the top, catching and eating air particulates.")]
internal class ItemMushroomCap : EquippableItem, IStatusAddedHandler {
  public void HandleStatusAdded(Status status) {
    if (status.isDebuff) {
      status.actor.Heal(1);
      status.Remove();
      stacks--;
    }
  }

  public override EquipmentSlot slot => EquipmentSlot.Headwear;

  public override int stacksMax => 3;
  public override bool disjoint => true;

  internal override string GetStats() => "If you'd get a debuff, prevent it and heal 1 HP instead.";
}

[Serializable]
[ObjectInfo("colored_transparent_packed_657", "")]
public class ItemKingshroomPowder : Item, ITargetedAction<Actor> {
  public override int stacksMax => 3;
  public override bool disjoint => true;


  public void Infect(Player player, Actor target) {
    if (target.IsNextTo(player)) {
      target.statuses.Add(new InfectedStatus());
      stacks--;
    }
  }

  public string TargettedActionName => "Infect";
  public string TargettedActionDescription => "Choose target.";
  public IEnumerable<Actor> Targets(Player player) => player.floor.AdjacentActors(player.pos).Where((a) => a != player && !(a is Boss));

  public void PerformTargettedAction(Player player, Entity e) {
    var target = (Actor) e;
    player.SetTasks(
      new ChaseTargetTask(player, target),
      new GenericPlayerTask(player, () => Infect(player, target))
    );
  }

  internal override string GetStats() => "Infect an adjacent creature. Each turn, it takes 1 damage and spawns a Thick Mushroom adjacent to it.";
}

[Serializable]
[ObjectInfo("infected")]
class InfectedStatus : Status {
  public override void Start() {
    actor.AddTimedEvent(1, IndependentStep);
  }
  void IndependentStep() {
    var tile = Util.RandomPick(actor.floor.GetAdjacentTiles(actor.pos).Where((t) => t.CanBeOccupied()));
    if (tile != null) {
      actor.floor.Put(new ThickMushroom(tile.pos));
    }
    actor.TakeDamage(1, GameModel.main.player);
    actor.AddTimedEvent(1, IndependentStep);
  }

  public override string Info() => "Each turn, take 1 damage and spawn a Thick Mushroom adjacent to you.";
  public override bool Consume(Status other) => true;
}

[Serializable]
[ObjectInfo("germ", "")]
internal class ItemGerm : Item, IUsable {
  public override int stacksMax => 4;
  public override bool disjoint => true;

  public void Use(Actor a) {
    // create a ring of mushrooms around you.
    foreach (var tile in a.floor.GetAdjacentTiles(a.pos).Where((t) => t.CanBeOccupied())) {
      a.floor.Put(new ThickMushroom(tile.pos));
    }
    AudioClipStore.main.summon.Play();
    stacks--;
  }


  internal override string GetStats() => "Spawn allied Thick Mushrooms around you. You can swap positions with them.";
}


[Serializable]
class ThickMushroom : Actor {
  public ThickMushroom(Vector2Int pos) : base(pos) {
    faction = Faction.Ally;
    hp = baseMaxHp = 3;
    timeNextAction += 999999;
    statuses.Add(new SporedStatus(20));
  }

  public override float Step() {
    return 999999;
  }
}