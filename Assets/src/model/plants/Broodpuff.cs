using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[Serializable]
/// Broodpuff - ideas are:
// puffs, poofs, color, photosynthesis, cute, leech seed/sucking life, happy
// once you use it - you get another seed
[PlantConfig(FloorsToMature = 3, WaterCost = 100)]
public class Broodpuff : Plant {
  [Serializable]
  class Mature : MaturePlantStage {
    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      harvestOptions.Add(new Inventory(
        new ItemLeecher(),
        new ItemLeecher()
      ));
      harvestOptions.Add(new Inventory(
        new ItemLeecher(3),
        new ItemBacillomyte()
      ));
      harvestOptions.Add(new Inventory(
        new ItemBroodleaf()
      ));
    }
  }

  public Broodpuff(Vector2Int pos) : base(pos) {
    stage.NextStage = new Mature();
  }
}

[Serializable]
[ObjectInfo(spriteName: "leecher")]
public class ItemLeecher : Item, IDurable, ITargetedAction<Tile> {

  internal override string GetStats() => "Summon a stationary ally. It attacks enemies for 1 damage (this uses Durability).\n\nYou may pickup the Leecher by tapping it.\n\nAt zero Durability, it becomes a Broodpuff Seed.";

  public int durability { get; set; }

  public int maxDurability => 6;

  public ItemLeecher(int durability = 6) {
    this.durability = durability;
  }

  public void Summon(Player player, Vector2Int position) {
    player.floor.Put(new Leecher(position, durability));
    AudioClipStore.main.summon.Play();
    Destroy();
  }

  public string TargettedActionName => "Summon";
  public string TargettedActionDescription => "Choose where to summon the Leecher.";
  public IEnumerable<Tile> Targets(Player player) =>
    player
      .GetVisibleTiles()
      .Where((p) => p.CanBeOccupied());

  public void PerformTargettedAction(Player player, Entity target) {
    player.task = new GenericPlayerTask(player, () => Summon(player, target.pos));
  }
}

[Serializable]
[ObjectInfo(spriteName: "leecher", description: "Attacking uses Durability.\nPickup this Leecher by tapping it.\nWhen out of Durability, turns into a Broodpuff Seed.")]
public class Leecher : AIActor, IAttackHandler {
  public override float turnPriority => 15;
  public override string description => base.description + $"\nDurability: {durability}/10";
  public int durability;
  public Leecher(Vector2Int pos, int durability) : base(pos) {
    this.durability = durability;
    hp = baseMaxHp = 1;
    faction = Faction.Ally;
    ClearTasks();
  }

  public void Pickup() {
    var player = GameModel.main.player;
    player.inventory.AddItem(new ItemLeecher(durability), this);
    // do NOT "kill" to prevent infinite triggers
    floor.Remove(this);
    // KillSelf();
  }

  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);
    if (!(source == this)) {
      floor.Put(new ItemOnGround(pos, new ItemLeecher(durability)));
    }
  }

  internal override (int, int) BaseAttackDamage() => (1, 1);

  protected override ActorTask GetNextTask() {
    var target = Util.RandomPick(floor.AdjacentActors(pos).Where(a => a.faction == Faction.Enemy));
    if (target != null) {
      return new AttackTask(this, target);
    } else {
      return new WaitTask(this, 1);
    }
  }

  public void OnAttack(int damage, Body target) {
    durability--;
    if (durability <= 0) {
      inventory.AddItem(new ItemSeed(typeof(Broodpuff)));
      KillSelf();
    }
  }
}

[Serializable]
[ObjectInfo("bacillomyte")]
public class ItemBacillomyte : Item, IUsable, IDurable {
  public ItemBacillomyte() {
    this.durability = maxDurability;
  }

  public int durability { get; set; }
  public int maxDurability => 4;
  internal override string GetStats() => "Use to grow Bacillomytes around you. Enemies standing over Bacillomyte take 1 extra attack damage.";

  public void Use(Actor a) {
    foreach (var tile in a.floor.GetAdjacentTiles(a.pos).Where(Bacillomyte.CanOccupy)) {
      a.floor.Put(new Bacillomyte(tile.pos));
    }
    this.ReduceDurability();
  }
}

[Serializable]
[ObjectInfo("bacillomyte", description: "Enemies standing over Bacillomyte take 1 extra attack damage.")]
public class Bacillomyte : Grass {
  public static bool CanOccupy(Tile tile) => tile is Ground;
  private class BacillomyteBodyModifier : IAttackDamageTakenModifier {
    public static BacillomyteBodyModifier instance = new BacillomyteBodyModifier();
    public int Modify(int input) {
      return input + 1;
    }
  }
  public Bacillomyte(Vector2Int pos) : base(pos) { }

  public override object BodyModifier => body is Actor a && a.faction != Faction.Ally ? BacillomyteBodyModifier.instance : null;
}

[Serializable]
[ObjectInfo("broodleaf", flavorText: "Dangerous toxins line the spiky tips of this otherwise unassuming leaf.")]
public class ItemBroodleaf : EquippableItem, IWeapon, IDurable, IAttackHandler, IActionCostModifier {
  internal override string GetStats() => "Applies the Vulnerable Status to attacked Creatures, making them take 1 more attack damage for 20 turns.\nAttacks twice as fast.";
  public override EquipmentSlot slot => EquipmentSlot.Weapon;
  public (int, int) AttackSpread => (1, 1);
  public int durability { get; set; }
  public int maxDurability => 25;
  public ItemBroodleaf() {
    durability = maxDurability;
  }

  public void OnAttack(int damage, Body target) {
    if (target is Actor a) {
      a.statuses.Add(new VulnerableStatus(20));
    }
  }

  public ActionCosts Modify(ActionCosts input) {
    input[ActionType.ATTACK] /= 2;
    return input;
  }
}

[Serializable]
[ObjectInfo("vulnerable")]
public class VulnerableStatus : StackingStatus, IAttackDamageTakenModifier {
  public override bool isDebuff => true;
  public VulnerableStatus(int stacks) : base(stacks) { }

  public override string Info() => $"Take 1 more attack damage.\n{this.stacks} turns remaining.";

  public override void Step() {
    stacks--;
  }

  public int Modify(int input) {
    return input + 1;
  }
}