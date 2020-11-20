using System;
using System.Collections.Generic;
using UnityEngine;

public class Bat : Actor {
  private IEnumerator<ActorAction[]> actionGenerator;
  public Bat(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    actionGenerator = ActionGenerator().GetEnumerator();
    OnPreStep += HandlePreStep;
  }

  void HandlePreStep() {
    if (action == null) {
      actionGenerator.MoveNext();
      SetActions(actionGenerator.Current);
    }
  }

  private IEnumerable<ActorAction[]> ActionGenerator() {
    while (true) {
      bool canSeePlayer = currentTile.visiblity == TileVisiblity.Visible;
      // hack - start attacking you once the player has vision
      if (canSeePlayer) {
        if (IsNextTo(GameModel.main.player)) {
          yield return new ActorAction[] {
            new AttackGroundAction(this, GameModel.main.player.pos, 1),
          };
        } else {
          yield return new ActorAction[] {
            new ChaseTargetAction(this, GameModel.main.player)
          };
        }
      } else {
        yield return new ActorAction[] {
          new MoveRandomlyAction(this)
        };
      }
    }
  }
}
