using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Attacks and moves slowly.\nBlocks 1 attack damage.\nLeaves a trail of Rubble.", flavorText: "Eager to prove himself, Aurogan managed to Will Life into the boulder on Boulder Hill. The Council was impressed, then horrified, then flattened.")]
public class Golem : AIActor, IBodyMoveHandler, IAttackDamageTakenModifier {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.ATTACK] = 2f,
    [ActionType.MOVE] = 2f,
  };

  protected override ActionCosts actionCosts => StaticActionCosts;
  public Golem(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 6;
  }

  public void HandleMove(Vector2Int pos, Vector2Int oldPos) {
    floor.Put(new Rubble(oldPos));
  }

  internal override (int, int) BaseAttackDamage() => (3, 4);

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

  public int Modify(int input) {
    return input - 1;
  }
}
