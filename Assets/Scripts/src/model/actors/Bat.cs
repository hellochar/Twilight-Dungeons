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
      ai.MoveNext();
      SetActions(ai.Current);
    }
  }
}

public class Bat : AIActor {
  public Bat(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    ai = ActionGenerator().GetEnumerator();
  }

  private IEnumerable<ActorAction> ActionGenerator() {
    while (true) {
      bool canSeePlayer = currentTile.visiblity == TileVisiblity.Visible;
      // hack - start attacking you once the player has vision
      if (canSeePlayer) {
        if (IsNextTo(GameModel.main.player)) {
          yield return new AttackGroundAction(this, GameModel.main.player.pos, 1);
        } else {
          yield return new ChaseTargetAction(this, GameModel.main.player);
        }
      } else {
        yield return new MoveRandomlyAction(this);
      }
    }
  }
}
