using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

[ObjectInfo(description: "Spins webs underneath itself.\nPrioritizes expanding its territory.\nAttacks deal no damage but apply Poison.\nAttacks anyone adjacent to it.")]
public class Spider : AIActor, IDealAttackDamageHandler {
  public Spider(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 5;
    ClearTasks();
    if (UnityEngine.Random.value < 0.1f) {
      inventory.AddItem(new ItemSpiderSandals(15));
    }
    // OnMove += HandleMove;
  }

  protected override ActorTask GetNextTask() {
    if (grass == null || !(grass is Web)) {
      return new GenericTask(this, (_) => {
        floor.Put(new Web(this.pos));
      });
    }

    var intruders = floor.AdjacentActors(pos).Where((actor) => !(actor is Spider));
    if (intruders.Any()) {
      var target = Util.RandomPick(intruders);
      return new AttackTask(this, target);
    }

    var nonWebbedAdjacentTiles = floor.GetAdjacentTiles(pos).Where((tile) => tile.CanBeOccupied() && !(tile.grass is Web));
    var webbedAdjacentTiles = floor.GetAdjacentTiles(pos).Where((tile) => tile.CanBeOccupied() && (tile.grass is Web));

    if (nonWebbedAdjacentTiles.Any()) {
      return new MoveToTargetTask(this, Util.RandomPick(nonWebbedAdjacentTiles).pos);
    } else {
      return new MoveToTargetTask(this, Util.RandomPick(webbedAdjacentTiles).pos);
    }
  }

  public void HandleDealAttackDamage(int dmg, Body target) {
    if (target is Actor a) {
      a.statuses.Add(new PoisonedStatus(1));
    }
  }

  internal override (int, int) BaseAttackDamage() => (0, 0);
}

internal class Web : Grass, IActorEnterHandler, IActorLeaveHandler {
  public Web(Vector2Int pos) : base(pos) { }

  protected override void HandleEnterFloor() {
    if (actor != null) {
      HandleActorEnter(actor);
    }
  }

  public void HandleActorEnter(Actor actor) {
    actor.statuses.Add(new WebStatus());
    OnNoteworthyAction();
  }

  public void HandleActorLeave(Actor actor) {
    if (!IsActorNice(actor)) {
      Kill();
    }
  }

  public static bool IsActorNice(Actor actor) {
    return actor is Spider spider || (actor is Player player && player.equipment[EquipmentSlot.Feet] is ItemSpiderSandals);
  }
}

[Serializable]
[ObjectInfo("spider-silk-shoes", "whoa")]
internal class ItemSpiderSandals : EquippableItem, IStackable, IBodyMoveHandler {
  public override EquipmentSlot slot => EquipmentSlot.Feet;
  public int stacksMax => 15;
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
  }

  public void HandleMove(Vector2Int pos, Vector2Int oldPos) {
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

  internal override string GetStats() => "Take no penalty from webs.\nLeave a trail of webs when you move.";
}

internal class WebStatus : Status, IActionCostModifier {
  public ActionCosts Modify(ActionCosts costs) {
    if (!Web.IsActorNice(actor)) {
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

  public override bool Consume(Status other) => true;

  public override string Info() => Web.IsActorNice(actor) ? "You're wearing Spider Sandals! No web penalty." : "Move twice as slow through webs!";
}

/// stacks = turns
[ObjectInfo("poisoned-status", "You feel sick to your stomach...")]
internal class PoisonedStatus : StackingStatus {
  int duration = 5;
  public PoisonedStatus(int stacks) : base() {
    this.stacks = stacks;
  }

  public override void Start() {
    actor.AddTimedEvent(1, IndependentStep);
  }

  public void IndependentStep() {
    if (stacks >= 3) {
      actor.AddTimedEvent(0.01f, TickDamage);
    }
    if (--duration <= 0) {
      --stacks;
      duration = 5;
    }
    if (stacks > 0) {
      actor.AddTimedEvent(1, IndependentStep);
    }
  }

  public void TickDamage() {
    actor.TakeDamage(3);
    stacks -= 3;
  }

  public override string Info() => $"At 3 stacks, take 3 damage and remove stacks.\nLose one stack every 5 turns.";
}