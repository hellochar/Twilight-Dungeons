using System;
using System.Collections.Generic;
using System.Linq;

public static class AIs {
  /// bats hide in corners and occasionally attack the closest target
  public static IEnumerable<ActorAction> BatAI(Actor actor) {
    Func<Actor> SelectTarget = () => {
      var potentialTargets = actor.floor
        .ActorsInCircle(actor.pos, 7)
        .Where((t) => actor.floor.TestVisibility(actor.pos, t.pos) && !(t is Bat));
      if (potentialTargets.Any()) {
        return potentialTargets.Aggregate((t1, t2) => actor.DistanceTo(t1) < actor.DistanceTo(t2) ? t1 : t2);
      }
      return null;
    };

    var roost = actor.pos;
    bool isHungry = true;
    actor.OnAttack += (int dmg, Actor target) => {
      isHungry = false;
    };

    yield return new SleepAction(actor);
    while (true) {
      if (!isHungry) {
        while (actor.pos != roost) {
          var moveToTarget = new MoveToTargetAction(actor, roost);
          // this means there's no path forward right now
          if (moveToTarget.IsDone()) {
            yield return new WaitAction(actor, 1);
          } else {
            yield return moveToTarget;
          }
        }
        yield return new WaitAction(actor, 7);
        isHungry = true;
      } else {
        var target = SelectTarget();
        if (target != null) {
          yield return new ChaseDynamicTargetAction(actor, SelectTarget, target);
          yield return new AttackAction(actor, target);
        } else {
          yield return new MoveRandomlyAction(actor);
        }
      }
    }

  }

  public static IEnumerable<ActorAction> BlobAI(Actor actor) {
    while (true) {
      bool canSeePlayer = actor.currentTile.visibility == TileVisiblity.Visible;
      // hack - start attacking you once the player has vision
      if (canSeePlayer) {
        if (actor.IsNextTo(GameModel.main.player)) {
          yield return new AttackGroundAction(actor, GameModel.main.player.pos, 1);
        } else {
          yield return new ChaseTargetAction(actor, GameModel.main.player);
        }
      } else {
        yield return new MoveRandomlyAction(actor);
      }
    }
  }

  public static IEnumerable<ActorAction> JackalAI(Actor actor) {
    yield return new SleepAction(actor);
    while (true) {
      bool canSeePlayer = actor.currentTile.visibility == TileVisiblity.Visible;
      if (canSeePlayer) {
        if (actor.IsNextTo(GameModel.main.player)) {
          yield return new AttackAction(actor, GameModel.main.player);
        } else {
          yield return new ChaseTargetAction(actor, GameModel.main.player);
        }
      } else {
        yield return new MoveRandomlyAction(actor);
      }
    }
  }
}