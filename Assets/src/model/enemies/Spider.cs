using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Moves randomly.\nSpins Webs underneath itself.")]
public class Spider : AIActor {
  // mark it as being capturable, but doesn't spawn a HomeItem
  public static Item HomeItem => null;
  public override void StepDay() {
    // create a nearby web
    var tile = Util.RandomPick(floor.GetDiagonalAdjacentTiles(pos).Where(Web.CanOccupy));
    if (tile != null) {
      floor.Put(new Web(tile.pos));
    }
  }

  public Spider(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 4;
    // OnMove += HandleMove;
  }

  private void DoNotRename_PutWeb() {
    if (Web.CanOccupy(tile)) {
      floor.Put(new Web(pos));
    }
  }

  protected override ActorTask GetNextTask() {
    if (Web.CanOccupy(tile)) {
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, DoNotRename_PutWeb));
    }

    // var intruders = new HashSet<Actor>(floor.AdjacentActors(pos).Where((actor) => !(actor is Spider)));
    // if (!CanTargetPlayer()) {
    //   intruders.Remove(GameModel.main.player);
    // }
    // if (intruders.Any()) {
    //   var target = Util.RandomPick(intruders);
    //   return new AttackTask(this, target);
    // }
    if (CanTargetPlayer() && IsNextTo(GameModel.main.player)) {
      return new AttackTask(this, GameModel.main.player);
    }

    var moves = new HashSet<Tile>(floor.GetAdjacentTiles(pos).Where(t => t.CanBeOccupied()));

    var nonWebbedMoves = moves.Where(Web.CanOccupy);
    var webbedMoves = moves.Except(nonWebbedMoves);

    if (nonWebbedMoves.Any()) {
      return new MoveToTargetTask(this, Util.RandomPick(nonWebbedMoves).pos);
    } else if (webbedMoves.Any()) {
      return new MoveToTargetTask(this, Util.RandomPick(webbedMoves).pos);
    } else {
      return new WaitTask(this, 1);
    }
  }

  // public void HandleDealAttackDamage(int dmg, Body target) {
  //   if (target is Actor a) {
  //     a.statuses.Add(new PoisonedStatus(1));
  //   }
  // }

  internal override (int, int) BaseAttackDamage() => (1, 1);
}

[System.Serializable]
[ObjectInfo(description: "Take +1 damage from the next attack. You must spend one turn to break the Web.")]
internal class Web : Grass, IActorEnterHandler {
  public static Item HomeItem => new SilkSandals(3);
  public static bool CanOccupy(Tile tile) => tile is Ground && !(tile.grass is Web);

  public Web(Vector2Int pos) : base(pos) {}

  [OnDeserialized]
  void HandleDeserialized() {
    if (BodyModifier != null) {
      // back-compat
      BodyModifier = null;
    }
  }

  protected override void HandleEnterFloor() {
    actor?.statuses.Add(new WebbedStatus());
  }

  protected override void HandleLeaveFloor() {
    actor?.statuses.RemoveOfType<WebbedStatus>();
  }

  public void HandleActorEnter(Actor actor) {
    actor.statuses.Add(new WebbedStatus());
    OnNoteworthyAction();
  }

  public static bool IsActorNice(Actor actor) {
    return actor is Spider spider || (actor is Player player && player.equipment[EquipmentSlot.Footwear] is SilkSandals);
  }
}

[Serializable]
[ObjectInfo("spider-silk-shoes", "Finely woven from the web of spiders.")]
internal class SilkSandals : EquippableItem, IBodyMoveHandler {
  public override EquipmentSlot slot => EquipmentSlot.Footwear;
  public override int stacksMax => int.MaxValue;

  public SilkSandals(int stacks) : base(stacks) {
  }

  public void HandleMove(Vector2Int pos, Vector2Int oldPos) {
    var player = GameModel.main.player;
    var oldTile = player.floor.tiles[oldPos];
    if (Web.CanOccupy(oldTile)) {
      player.floor.Put(new Web(oldPos));
      stacks--;
    }
    if (stacks > 0 && Web.CanOccupy(player.tile)) {
      player.floor.Put(new Web(player.pos));
      stacks--;
    }
  }

  internal override string GetStats() => "Take no penalty from webs.\nLeave a trail of webs when you move.";
}

[System.Serializable]
internal class WebbedStatus : Status, IAttackDamageTakenModifier, IBaseActionModifier, IDeathHandler {
  public override bool isDebuff => !Web.IsActorNice(actor);
  private Web web => actor?.grass as Web;

  public WebbedStatus() {
  }

  public override void End() {
    web?.Kill(actor);
  }

  public override void Step() {
    if (web == null) {
      Remove();
    }
  }

  public override bool Consume(Status other) => true;

  public override string Info() => Web.IsActorNice(actor) ? "You're wearing Spider Sandals! No web penalty." : "Prevents your next movement.";

  public int Modify(int input) {
    if (web == null) {
      return input;
    }
    if (Web.IsActorNice(web.actor)) {
      return input;
    }
    Remove();
    return input + 1;
  }

  public void HandleDeath(Entity source) {
    if (!Web.IsActorNice(actor)) {
      var web = this.web;
      if (web != null) {
        GameModel.main.EnqueueEvent(() => {
          web.Kill(actor);
        });
      }
    }
  }

  public BaseAction Modify(BaseAction input) {
    if (web == null) {
      Remove();
      return input;
    }
    if (Web.IsActorNice(web.actor)) {
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
    // Debug.Log(actor + ", stacks " + stacks + ", " + GameModel.main.time);
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
    // Debug.Log("Ticking damage on " + actor + ", new stacks " + stacks + ", " + GameModel.main.time);
    if (stacks > 0) {
      // match timing back up
      actor.AddTimedEvent(0.99f, IndependentStep);
    }
  }

  public override string Info() => $"At 3 stacks, take 3 damage and remove 3 stacks.\nLose one stack every 5 turns.";
}