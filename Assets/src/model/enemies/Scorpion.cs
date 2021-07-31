using System;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Chases you.\nAttacks and moves twice.")]
public class Scorpion : AIActor {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.MOVE] = 0.5f,
    [ActionType.ATTACK] = 0.5f,
  };

  protected override ActionCosts actionCosts => StaticActionCosts;
  public Scorpion(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 5;
  }

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

  internal override (int, int) BaseAttackDamage() => (1, 1);
}
