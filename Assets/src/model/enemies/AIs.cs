using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

public static class AIs {
  // bugs just walk around, or sit still, who knows
  internal static IEnumerable<ActorTask> BugAI(Actor actor) {
    while (true) {
      if (Random.value < 0.5f) {
        yield return new WaitTask(actor, Random.Range(1, 5));
      } else {
        var range5Tiles = actor.floor.EnumerateCircle(actor.pos, 5).Where((pos) => actor.floor.tiles[pos].CanBeOccupied());
        var target = Util.RandomPick(range5Tiles);
        yield return new MoveToTargetTask(actor, target);
      }
    }
  }

  public static IEnumerable<ActorTask> JackalAI(Actor actor) {
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