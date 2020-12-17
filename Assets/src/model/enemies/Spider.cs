using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

// move slow, but cover ground with webs
public class Spider : AIActor {

  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.ATTACK] = 3,
    [ActionType.MOVE] = 2,
  };

  protected override ActionCosts actionCosts => Spider.StaticActionCosts;

  public Spider(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = hpMax = 8;
    ai = AI().GetEnumerator();
    OnDealDamage += HandleDealDamage;
    // OnMove += HandleMove;
  }

  private IEnumerable<ActorTask> AI() {
    while (true) {
      if (grass == null || !(grass is Web)) {
        yield return new TelegraphedTask(this, 4, new GenericBaseAction(this, (_) => {
          floor.Put(new Web(this.pos));
        }));
        // yield return new GenericTask(this, (_) => {
        //   floor.Put(new Web(this.pos));
        // });
        continue;
      }

      var intruders = floor.AdjacentActors(pos).Where((actor) => !(actor is Spider));
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
        // prefer to walk on their web 6 to 1
        bag.Add(6, t);
      }
      var nextTile = bag.GetRandom();

      yield return new MoveToTargetTask(this, nextTile.pos);
    }
  }

  private void HandleDealDamage(int dmg, Actor target) {
    target.statuses.Add(new PoisonedStatus(25));
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
  }

  void HandleLeaveFloor() {
    tile.OnActorEnter -= HandleActorEnter;
    tile.OnActorLeave -= HandleActorLeave;
  }

  void HandleActorEnter(Actor actor) {
    if (actor is Spider) {
      actor.statuses.Add(new WebStatus());
    }
    TriggerNoteworthyAction();
  }

  private void HandleActorLeave(Actor actor) {
    if (!(actor is Spider)) {
      actor.statuses.Add(new PoisonedStatus(5));
      Kill();
    }
  }
}

internal class WebStatus : Status, IActionCostModifier {
  public ActionCosts Modify(ActionCosts costs) {
    if (actor is Spider) {
      // twice as fast (aka normal speed)
      costs[ActionType.MOVE] /= 2;
    } else {
      // 100% slower
      costs[ActionType.MOVE] *= 2;
    }
    return costs;
  }

  public override void Step() {
    if (!(actor.grass is Web)) {
      Remove();
    }
  }

  public override void Stack(Status other) { }

  public override string Info() => "You move twice as slow, but Spiders move normal speed!";
}

/// stacks = turns
internal class PoisonedStatus : StackingStatus {
  private int countDown = 20;

  public PoisonedStatus(int stacks) : base() {
    this.stacks = stacks;
  }

  public override void Step() {
    if (--countDown <= 0) {
      countDown = 20;
      if (actor.hp > 1) {
        actor.TakeDamage(1, actor);
      }
    }
    --stacks;
  }

  public override string Info() => $"Deals 1 non-lethal damage every 20 turns.\n{stacks} turns remaining.";
}