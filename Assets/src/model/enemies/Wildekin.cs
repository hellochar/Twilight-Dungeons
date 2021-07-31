using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Runs away for one turn after attacking.", flavorText: "")]
public class Wildekin : AIActor, IAttackHandler {
  public Wildekin(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 8;
  }

  internal override (int, int) BaseAttackDamage() => (1, 2);

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;
    if (CanTargetPlayer()) {
      if (IsNextTo(player)) {
        return new AttackTask(this, player);
      } else {
        return new ChaseTargetTask(this, player);
      }
    } else {
      return new MoveRandomlyTask(this);
    }
  }

  public void OnAttack(int damage, Body target) {
    if (target is Actor a) {
      SetTasks(new RunAwayTask(this, target.pos, 1, false));
    }
  }
}
