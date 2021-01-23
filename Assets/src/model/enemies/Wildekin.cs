using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ObjectInfo(description: "Runs away for one turn after attacking.", flavorText: "")]
public class Wildekin : AIActor {
  public Wildekin(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 8;
    ai = AI().GetEnumerator();
  }

  internal override (int, int) BaseAttackDamage() => (1, 2);

  private IEnumerable<ActorTask> AI() {
    var player = GameModel.main.player;
    while (true) {
      if (isVisible) {
        if (IsNextTo(player)) {
          yield return new AttackTask(this, player);
          yield return new RunAwayTask(this, player.pos, 1, false);
        } else {
          yield return new ChaseTargetTask(this, player);
        }
      } else {
        yield return new WaitTask(this, 1);
      }
    }
  }
}
