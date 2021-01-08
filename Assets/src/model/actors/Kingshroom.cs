using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class Kingshroom : Plant {
  public override int maxWater => 3;
  class Mature : PlantStage {
    public override float StepTime => 99999;
    public override void Step() { }
    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Kingshroom)),
        new ItemSeed(typeof(Kingshroom)),
        new ItemMushroom(20)
      ));
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Kingshroom)),
        new ItemKingshroomHat(),
        new ItemKingshroomPowder()
      ));
      harvestOptions.Add(new Inventory(
        new ItemBodyMycelium()
      ));
    }
    public override string getUIText() => $"Ready to harvest.";
  }

  public Kingshroom(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Mature();
  }
}

[ObjectInfo("kingshroom", "")]
internal class ItemBodyMycelium : EquippableItem, IDurable, IAttackHandler, IActionPerformedHandler, IModifierProvider {
  public override EquipmentSlot slot => EquipmentSlot.Body;
  public ItemBodyMycelium() {
    durability = maxDurability;
  }

  public int durability { get; set; }

  public int maxDurability => 300;

  private class DealLessDamage : IAttackDamageModifier {
    public int Modify(int input) => input - 1;
  }
  private class TakeLessDamage : IAttackDamageTakenModifier {
    public int Modify(int input) => input - 2;
  }

  private static List<object> Modifiers = new List<object> { new TakeLessDamage(), new DealLessDamage() };
  public IEnumerable<object> MyModifiers => Modifiers;

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (final.Type == ActionType.MOVE) {
      this.ReduceDurability();
    } else if (final.Type == ActionType.WAIT) {
      this.IncreaseDurability();
    }
  }

  public void OnAttack(Actor target) {
    var floor = target.floor;
    var pos = target.pos;
    GameModel.main.EnqueueEvent(() => {
      if (target.IsDead) {
        floor.Put(new ThickMushroom(pos));
      }
    });
  }

  internal override string GetStats() => "Take 2 less attack damage.\nDeal 1 less attack damage.\nMoving reduces durability.\nWaiting increases durability.\nKilling an enemy creates a Mushroom in its place.";

  public override List<MethodInfo> GetAvailableMethods(Player actor) {
    var methods = base.GetAvailableMethods(actor);
    methods.Remove(GetType().GetMethod("Unequip"));
    methods.Remove(GetType().GetMethod("Drop"));
    return methods;
  }
}

[ObjectInfo("colored_transparent_packed_34", "")]
internal class ItemKingshroomHat : EquippableItem, IDurable, IActionPerformedHandler {
  public ItemKingshroomHat() {
    durability = maxDurability;
  }

  public override EquipmentSlot slot => EquipmentSlot.Head;

  public int durability { get; set; }

  public int maxDurability => 30;

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (final.Type == ActionType.WAIT && !player.statuses.Has<GerminatingStatus>()) {
      player.statuses.Add(new GerminatingStatus());
      this.ReduceDurability();
    }
  }

  internal override string GetStats() => "When you stand still for 3 turns, emit toxic spores.";
}

[ObjectInfo("colored_transparent_packed_861", "")]
class GerminatingStatus : StackingStatus, IActionPerformedHandler {
  public override StackingMode stackingMode => StackingMode.Add;
  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (final.Type == ActionType.WAIT) {
      stacks++;
      if (stacks >= 3) {
        foreach (var a in actor.floor.AdjacentActors(actor.pos).Where((who) => who != actor)) {
          a.statuses.Add(new SporedStatus(20));
        }
        stacks = 0;
      }
    } else {
      stacks = 0;
    }
  }

  public override string Info() => "Something is growing on your head!";
}

[ObjectInfo("colored_transparent_packed_657", "")]
internal class ItemKingshroomPowder : Item, IDurable, IUsable {
  public ItemKingshroomPowder() {
    durability = maxDurability;
  }

  public int durability { get; set; }

  public int maxDurability => 10;

  public void Use(Actor a) {
    // create a ring of mushrooms around you.
    foreach (var tile in a.floor.GetAdjacentTiles(a.pos).Where((t) => t.CanBeOccupied())) {
      a.floor.Put(new ThickMushroom(tile.pos));
    }
    this.ReduceDurability();
  }

  internal override string GetStats() => "Create friendly mushrooms. They do nothing for 3-6 turns and then explode, dealing 1 damage to adjacent enemies.";
}

class ThickMushroom : AIActor {
  public ThickMushroom(Vector2Int pos) : base(pos) {
    faction = Faction.Ally;
    hp = baseMaxHp = 3;
    ClearTasks();
    ai = AI().GetEnumerator();
  }

  public IEnumerable<ActorTask> AI() {
    // yield return new WaitTask(this, 30);
    while(true) {
      yield return new WaitTask(this, UnityEngine.Random.Range(3, 7));
      GameModel.main.EnqueueEvent(() => {
        actor.statuses.Add(new SurprisedStatus());
      });
      yield return new WaitTask(this, 1);
      yield return new GenericTask(this, (_) => {
        ReleaseSpores();
      });
    }
  }

  public void ReleaseSpores() {
    foreach (var actor in floor.AdjacentActors(pos).Where((actor) => actor.faction == Faction.Enemy)) {
      actor.TakeDamage(1);
    }
    Kill();
  }
}