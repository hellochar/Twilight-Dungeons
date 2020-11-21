using System.Collections.Generic;

public static class AIs {
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
        yield return new ChaseTargetAction(actor, GameModel.main.player);
        yield return new AttackAction(actor, GameModel.main.player);
      } else {
        yield return new MoveRandomlyAction(actor);
      }
    }
  }
}