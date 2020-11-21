using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// An actor whose actions are controlled by some sort of AI.
/// This AI decides what actions the actor takes.
/// TODO we should use composition for this instead, eventually
public class AIActor : Actor {
  protected IEnumerator<ActorAction> ai;
  public AIActor(Vector2Int pos) : base(pos) {
    OnPreStep += HandlePreStep;
  }

  void HandlePreStep() {
    if (action == null) {
      do {
        ai.MoveNext();
      } while (ai.Current.IsDone());
      SetActions(ai.Current);
    }
  }
}

public class Bat : AIActor {
  public Bat(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    ai = AIs.BatAI(this).GetEnumerator();
  }
}

// run fast
public class Jackal : AIActor {
  public static new IDictionary<Type, float> ActionCosts = new ReadOnlyDictionary<Type, float>(
    new Dictionary<Type, float>(Actor.ActionCosts) {
      {typeof(FollowPathAction), 0.5f}
    }
  );
  public override IDictionary<Type, float> actionCosts => Jackal.ActionCosts;
  public Jackal(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    ai = AIs.JackalAI(this).GetEnumerator();
  }
}

public static class AIs {
  public static IEnumerable<ActorAction> BatAI(Actor actor) {
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
    while (true) {
      bool canSeePlayer = actor.currentTile.visibility == TileVisiblity.Visible;
      if (canSeePlayer) {
        yield return new ChaseTargetAction(actor, GameModel.main.player);
        yield return new AttackAction(actor, GameModel.main.player);
      } else {
        yield return new WaitAction(actor, 1);
      }
    }
  }

}