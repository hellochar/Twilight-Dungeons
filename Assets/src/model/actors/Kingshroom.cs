using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[Serializable]
public class Kingshroom : Plant {
  public override int maxWater => 3;
  [Serializable]
  class Mature : PlantStage {
    public override float StepTime => 99999;
    public override void Step() { }
    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Kingshroom)),
        new ItemSeed(typeof(Kingshroom)),
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
    public override string getUIText() => $"Ready to harvest.";
  }

  public Kingshroom(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Mature();
  }
}

[Serializable]
[ObjectInfo("living-armor", "")]
internal class ItemLivingArmor : EquippableItem, ISticky, IDurable, IActionPerformedHandler, IAttackDamageTakenModifier {
  public override EquipmentSlot slot => EquipmentSlot.Body;
  public ItemLivingArmor() {
    durability = maxDurability;
  }

  public int durability { get; set; }

  public int maxDurability => 300;

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (final.Type == ActionType.MOVE) {
      this.ReduceDurability();
    }
  }

  public int Modify(int input) => input - 2;

  internal override string GetStats() => "Blocks 2 attack damage.\nMoving reduces durability.\nCannot be unequipped.";
}

[Serializable]
[ObjectInfo("mushroom-cap", "Spongy and porous holes dot the top, catching and eating air particulates.")]
internal class ItemMushroomCap : EquippableItem, IDurable, IStatusAddedHandler {
  public ItemMushroomCap() {
    durability = maxDurability;
  }

  public void HandleStatusAdded(Status status) {
    if (status is SporedStatus) {
      status.actor.Heal(1);
      status.Remove();
      this.ReduceDurability();
    }
  }

  public override EquipmentSlot slot => EquipmentSlot.Head;

  public int durability { get; set; }

  public int maxDurability => 5;

  internal override string GetStats() => "If you'd get the Spored Status, prevent it and heal 1 HP instead.";
}

[Serializable]
[ObjectInfo("colored_transparent_packed_657", "")]
public class ItemKingshroomPowder : Item, IDurable {
  public ItemKingshroomPowder() {
    durability = maxDurability;
  }

  public int durability { get; set; }

  public int maxDurability => 3;

  public void Infect(Player player, Actor target) {
    if (target.IsNextTo(player)) {
      target.statuses.Add(new InfectedStatus());
      this.ReduceDurability();
    }
  }

  internal override string GetStats() => "Infect an adjacent creature. Each turn, it takes 1 damage and spawns a Thick Mushroom adjacent to it.";
}

[Serializable]
[ObjectInfo("infected")]
class InfectedStatus : Status {
  public override void Step() {
    var tile = Util.RandomPick(actor.floor.GetAdjacentTiles(actor.pos).Where((t) => t.CanBeOccupied()));
    if (tile != null) {
      actor.floor.Put(new ThickMushroom(tile.pos));
    }
    GameModel.main.EnqueueEvent(() => actor.TakeDamage(1));
  }

  public override string Info() => "Each turn, take 1 damage and spawn a Thick Mushroom adjacent to you.";
  public override bool Consume(Status other) => true;
}

[Serializable]
[ObjectInfo("germ", "")]
internal class ItemGerm : Item, IDurable, IUsable {
  public ItemGerm() {
    durability = maxDurability;
  }

  public int durability { get; set; }

  public int maxDurability => 7;

  public void Use(Actor a) {
    // create a ring of mushrooms around you.
    foreach (var tile in a.floor.GetAdjacentTiles(a.pos).Where((t) => t.CanBeOccupied())) {
      a.floor.Put(new ThickMushroom(tile.pos));
    }
    this.ReduceDurability();
  }


  internal override string GetStats() => "Spawn allied Thick Mushrooms around you. You can swap positions with them.";
}


class ThickMushroom : Actor {
  public ThickMushroom(Vector2Int pos) : base(pos) {
    faction = Faction.Ally;
    hp = baseMaxHp = 3;
    timeNextAction += 999999;
  }

  public override float Step() {
    return 999999;
  }
}