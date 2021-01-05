using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

// move slow, but cover ground with webs
public class Spider : AIActor {

  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.MOVE] = 2,
  };

  protected override ActionCosts actionCosts => Spider.StaticActionCosts;

  public Spider(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 7;
    ClearTasks();
    ai = AI().GetEnumerator();
    OnDealAttackDamage += HandleDealDamage;
    if (UnityEngine.Random.value < 0.25f) {
      inventory.AddItem(new ItemSpiderSandals(30));
    }
    // OnMove += HandleMove;
  }

  private IEnumerable<ActorTask> AI() {
    while (true) {
      if (grass == null || !(grass is Web)) {
        yield return new GenericTask(this, (_) => {
          floor.Put(new Web(this.pos));
        });
        continue;
      }

      var intruders = floor.AdjacentActors(pos).Where((actor) => !(actor is Spider) && !(actor is Plant) && !(actor is Rubble));
      if (intruders.Any()) {
        var target = Util.RandomPick(intruders);
        yield return new AttackTask(this, target);
        continue;
      }

      var nonWebbedAdjacentTiles = floor.GetAdjacentTiles(pos).Where((tile) => tile.CanBeOccupied() && !(tile.grass is Web));
      var webbedAdjacentTiles = floor.GetAdjacentTiles(pos).Where((tile) => tile.CanBeOccupied() && (tile.grass is Web));

      var bag = new WeightedRandomBag<Tile>();
      foreach (var t in nonWebbedAdjacentTiles) {
        bag.Add(1, t);
      }
      foreach (var t in webbedAdjacentTiles) {
        // prefer to walk on their web 3 to 1
        bag.Add(3, t);
      }
      var nextTile = bag.GetRandom();

      yield return new MoveToTargetTask(this, nextTile.pos);
    }
  }

  private void HandleDealDamage(int dmg, Actor target) {
    target.statuses.Add(new PoisonedStatus(1));
  }

  internal override int BaseAttackDamage() {
    return 0;
  }
}

internal class Web : Grass {
  public Web(Vector2Int pos) : base(pos) {
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
  }

  void HandleEnterFloor() {
    tile.OnActorEnter += HandleActorEnter;
    tile.OnActorLeave += HandleActorLeave;
    if (actor != null) {
      HandleActorEnter(actor);
    }
  }

  void HandleLeaveFloor() {
    tile.OnActorEnter -= HandleActorEnter;
    tile.OnActorLeave -= HandleActorLeave;
  }

  void HandleActorEnter(Actor actor) {
    actor.statuses.Add(new WebStatus());
    TriggerNoteworthyAction();
  }

  public static bool IsActorNice(Actor actor) {
    return actor is Spider spider || (actor is Player player && player.equipment[EquipmentSlot.Feet] is ItemSpiderSandals);
  }

  private void HandleActorLeave(Actor actor) {
    if (!IsActorNice(actor)) {
      Kill();
    }
  }
}

[ObjectInfo("spider-silk-shoes", "whoa")]
internal class ItemSpiderSandals : EquippableItem, IStackable {
  public override EquipmentSlot slot => EquipmentSlot.Feet;
  public int stacksMax => 30;
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

  public ItemSpiderSandals(int stacks) {
    this.stacks = stacks;
    OnEquipped += HandleEquipped;
    OnUnequipped += HandleUnequipped;
  }

  private void HandleEquipped(Player obj) {
    obj.OnMove += HandleMove;
  }

  private void HandleUnequipped(Player obj) {
    obj.OnMove -= HandleMove;
  }

  private void HandleMove(Vector2Int pos, Vector2Int oldPos) {
    var player = GameModel.main.player;
    if (!(player.floor.grasses[oldPos] is Web)) {
      player.floor.Put(new Web(oldPos));
      stacks--;
    }
    if (!(player.grass is Web) && stacks > 0) {
      player.floor.Put(new Web(player.pos));
      stacks--;
    }
  }

  internal override string GetStats() => "Move faster on webs.\nLeave a trail of webs when you move.";
}

internal class WebStatus : Status, IActionCostModifier {
  public ActionCosts Modify(ActionCosts costs) {
    if (Web.IsActorNice(actor)) {
      costs[ActionType.MOVE] /= 2;
    } else {
      costs[ActionType.MOVE] *= 2;
    }
    return costs;
  }

  public override void Step() {
    if (actor.floor == null) {
      throw new Exception("Stepping Web status even though the actor's left the floor!");
    }
    if (!(actor.grass is Web)) {
      Remove();
    }
  }

  public override void Stack(Status other) { }

  public override string Info() => Web.IsActorNice(actor) ? "You're wearing Spider Sandals! Move twice as fast through webs." : "Move twice as slow through webs!";
}

/// stacks = turns
[ObjectInfo("poisoned-status", "You feel sick to your stomach...")]
internal class PoisonedStatus : StackingStatus {
  int duration = 5;
  public PoisonedStatus(int stacks) : base() {
    this.stacks = stacks;
  }

  public override void Step() {
    if (stacks >= 3) {
      actor.TakeDamage(3);
      stacks = 0;
    }
    if (--duration <= 0) {
      --stacks;
      duration = 5;
    }
  }

  public override string Info() => $"At 3 stacks, take 3 damage and remove stacks.\nLose one stack every 5 turns.";
}