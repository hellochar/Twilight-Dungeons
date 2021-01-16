using System;
using System.Collections.Generic;
using UnityEngine;

[ObjectInfo(description: "Chases you.\nTelegraphs attacks.")]
public class Blob : AIActor {
  public override float turnPriority => task is AttackGroundTask ? 90 : base.turnPriority;
  public Blob(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 8;
    faction = Faction.Enemy;
    ai = BlobAI(this).GetEnumerator();
  }

  internal override (int, int) BaseAttackDamage() {
    return (2, 3);
  }

  public static IEnumerable<ActorTask> BlobAI(Actor actor) {
    while (true) {
      if (actor.isVisible) {
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
}
