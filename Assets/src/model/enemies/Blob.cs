using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Telegraphs attacks for 1 turn.\nChases you.", flavorText: "An odorless mass advances towards you with a steady, brainless determination.")]
public class Blob : AIActor {
  public override float turnPriority => task is AttackGroundTask ? 90 : base.turnPriority;
  public Blob(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 6;
    faction = Faction.Enemy;
  }

  internal override (int, int) BaseAttackDamage() {
    return (2, 3);
  }

  protected override ActorTask GetNextTask() {
    if (CanTargetPlayer()) {
      if (IsNextTo(GameModel.main.player)) {
        return new AttackGroundTask(this, GameModel.main.player.pos, 1);
      } else {
        return new ChaseTargetTask(this, GameModel.main.player);
      }
    } else {
      return new MoveRandomlyTask(this);
    }
  }
}
