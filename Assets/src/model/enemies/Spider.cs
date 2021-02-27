using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Spins webs underneath itself.\nPrioritizes expanding its territory.\nAttacks deal no damage but apply Poison.\nAttacks anyone adjacent to it.")]
public class Spider : AIActor, IDealAttackDamageHandler {
  public Spider(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 5;
    ClearTasks();
    if (MyRandom.value < 0.1f) {
      inventory.AddItem(new ItemSpiderSandals(15));
    }
    // OnMove += HandleMove;
  }

  private void DoNotRename_PutWeb() {
    floor.Put(new Web(pos));
  }

  protected override ActorTask GetNextTask() {
    if (grass == null || !(grass is Web)) {
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, DoNotRename_PutWeb));
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
    } else if (webbedAdjacentTiles.Any()) {
      return new MoveToTargetTask(this, Util.RandomPick(webbedAdjacentTiles).pos);
    } else {
      return new WaitTask(this, 1);
    }
  }

  public void HandleDealAttackDamage(int dmg, Body target) {
    if (target is Actor a) {
      a.statuses.Add(new PoisonedStatus(1));
    }
  }

  internal override (int, int) BaseAttackDamage() => (0, 0);
}

[System.Serializable]
[ObjectInfo(description: "Prevents movement; non-Spider creatures must spend one turn breaking the web.")]
internal class Web : Grass, IActorEnterHandler, IActorLeaveHandler {
  public Web(Vector2Int pos) : base(pos) { }

  protected override void HandleEnterFloor() {
    if (actor != null) {
      HandleActorEnter(actor);
    }
  }

  protected override void HandleLeaveFloor() {
    base.HandleLeaveFloor();
    if (status != null && status.actor != null) {
      status.Remove();
    }
  }

  private WebStatus status;
  public void HandleActorEnter(Actor actor) {
    status = new WebStatus(this);
    actor.statuses.Add(status);
    OnNoteworthyAction();
  }

  public void HandleActorLeave(Actor actor) {
    if (!IsActorNice(actor)) {
      Kill(actor);
    }
  }

  public static bool IsActorNice(Actor actor) {
    return actor is Spider spider || (actor is Player player && player.equipment[EquipmentSlot.Footwear] is ItemSpiderSandals);
  }

  internal void WebRemoved(Actor actor) {
    if (!IsActorNice(actor)) {
      Kill(actor);
    }
  }
}

[Serializable]
[ObjectInfo("spider-silk-shoes", "Finely woven from the web of spiders.")]
internal class ItemSpiderSandals : EquippableItem, IStackable, IBodyMoveHandler {
  public override EquipmentSlot slot => EquipmentSlot.Footwear;
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

[System.Serializable]
internal class WebStatus : Status, IBaseActionModifier {
  public override bool isDebuff => !Web.IsActorNice(actor);
  Web owner;

  public WebStatus(Web owner) {
    this.owner = owner;
  }

  public override void End() {
    owner.WebRemoved(actor);
  }

  public override void Step() {
    if (!(actor.grass is Web)) {
      Remove();
    }
  }

  public override bool Consume(Status other) => true;

  public override string Info() => Web.IsActorNice(actor) ? "You're wearing Spider Sandals! No web penalty." : "Prevents your next movement.";

  public BaseAction Modify(BaseAction input) {
    if (Web.IsActorNice(actor)) {
      return input;
    }
    if (input.Type == ActionType.MOVE) {
      Remove();
      return new StruggleBaseAction(input.actor);
    }
    return input;
  }
}

/// stacks = turns
[System.Serializable]
[ObjectInfo("poisoned-status", "You feel sick to your stomach...")]
internal class PoisonedStatus : StackingStatus {
  public override bool isDebuff => true;
  int duration = 5;
  public PoisonedStatus(int stacks) : base() {
    this.stacks = stacks;
  }

  public override void Start() {
    actor.AddTimedEvent(1, IndependentStep);
  }

  public void IndependentStep() {
    if (stacks >= 3) {
      /// trigger right after your turn, unless you move slowly, in which case it should just trigger immediately
      actor.AddTimedEvent(0.01f, TickDamage);
    } else {
      if (--duration <= 0) {
        --stacks;
        duration = 5;
      }
      if (stacks > 0) {
        actor.AddTimedEvent(1, IndependentStep);
      }
    }
  }

  public void TickDamage() {
    actor.TakeDamage(3, actor);
    stacks -= 3;
    if (stacks > 0) {
      // match timing back up
      actor.AddTimedEvent(0.99f, IndependentStep);
    }
  }

  public override string Info() => $"At 3 stacks, take 3 damage and remove 3 stacks.\nLose one stack every 5 turns.";
}