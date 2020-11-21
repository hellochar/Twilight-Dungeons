using System;
using System.Collections.Generic;
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
  public Jackal(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    ai = AIs.JackalAI(this).GetEnumerator();
  }
}

public static class AIs {
  public static IEnumerable<ActorAction> BatAI(Actor actor) {
    while (true) {
      bool canSeePlayer = actor.currentTile.visiblity == TileVisiblity.Visible;
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

    }
  }
}