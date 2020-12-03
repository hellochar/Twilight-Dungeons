using System;
using System.Collections.Generic;
using System.Linq;

public static class AIs {
  /// bats hide in corners and occasionally attack the closest target
  public static IEnumerable<ActorTask> BatAI(Actor actor) {
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

    yield return new SleepTask(actor);
    while (true) {
      if (!isHungry) {
        while (actor.pos != roost) {
          var moveToTarget = new MoveToTargetTask(actor, roost);
          // this means there's no path forward right now
          if (moveToTarget.path.Count == 0) {
            yield return new WaitTask(actor, 1);
          } else {
            yield return moveToTarget;
          }
        }
        yield return new WaitTask(actor, 7);
        isHungry = true;
      } else {
        var target = SelectTarget();
        if (target != null) {
          yield return new ChaseDynamicTargetTask(actor, SelectTarget, target);
          yield return new AttackTask(actor, target);
        } else {
          yield return new MoveRandomlyTask(actor);
        }
      }
    }

  }

  public static IEnumerable<ActorTask> BlobAI(Actor actor) {
    while (true) {
      bool canSeePlayer = actor.tile.visibility == TileVisiblity.Visible;
      // hack - start attacking you once the player has vision
      if (canSeePlayer) {
        if (actor.IsNextTo(GameModel.main.player)) {
          yield return new AttackGroundTask(actor, GameModel.main.player.pos, 1);
        } else {
          yield return new ChaseTargetTask(actor, GameModel.main.player);
        }
      } else {
        yield return new MoveRandomlyTask(actor);
      }
    }
  }

  public static IEnumerable<ActorTask> JackalAI(Actor actor) {
    yield return new SleepTask(actor);
    while (true) {
      bool canSeePlayer = actor.tile.visibility == TileVisiblity.Visible;
      if (canSeePlayer) {
        if (actor.IsNextTo(GameModel.main.player)) {
          yield return new AttackTask(actor, GameModel.main.player);
        } else {
          while (!actor.IsNextTo(GameModel.main.player)) {
            yield return new ChaseTargetTask(actor, GameModel.main.player);
          }
        }
      } else {
        yield return new MoveRandomlyTask(actor);
      }
    }
  }
}