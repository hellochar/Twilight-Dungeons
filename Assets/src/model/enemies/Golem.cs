using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Golem : AIActor, IAttackDamageTakenModifier {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.ATTACK] = 2f,
    [ActionType.MOVE] = 2f,
  };

  protected override ActionCosts actionCosts => StaticActionCosts;
  public Golem(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 10;
    ai = AI().GetEnumerator();
    OnMove += HandleMove;
  }

  private void HandleMove(Vector2Int pos, Vector2Int oldPos) {
    floor.Put(new Rubble(oldPos, 1));
  }

  internal override int BaseAttackDamage() {
    return UnityEngine.Random.Range(4, 6);
  }

  private IEnumerable<ActorTask> AI() {
    while (true) {
      if (isVisible) {
        if (IsNextTo(GameModel.main.player)) {
          yield return new AttackTask(actor, GameModel.main.player);
        } else {
          yield return new ChaseTargetTask(actor, GameModel.main.player);
        }
      } else {
        yield return new MoveRandomlyTask(actor);
      }
    }
  }

  public int Modify(int input) {
    return input - 1;
  }
}
