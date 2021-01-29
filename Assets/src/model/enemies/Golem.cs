using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ObjectInfo(description: "Attacks and moves take twice as long.\nTakes 1 less damage from attacks.\nLeaves a trail of Rubble.", flavorText: "Eager to prove himself, Aurogan had managed to Will Life into the boulder on the Boulder Hill. The Council was impressed; then horrified; then flat.")]
public class Golem : AIActor, IBodyMoveHandler, IAttackDamageTakenModifier {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.ATTACK] = 2f,
    [ActionType.MOVE] = 2f,
  };

  protected override ActionCosts actionCosts => StaticActionCosts;
  public Golem(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 10;
    ai = AI().GetEnumerator();
  }

  public void HandleMove(Vector2Int pos, Vector2Int oldPos) {
    floor.Put(new Rubble(oldPos, 1));
  }

  internal override (int, int) BaseAttackDamage() => (4, 5);

  private IEnumerable<ActorTask> AI() {
    var player = GameModel.main.player;
    while (true) {
      if (isVisible) {
        if (IsNextTo(player)) {
          yield return new AttackTask(this, player);
        } else {
          yield return new ChaseTargetTask(this, player);
        }
      } else {
        yield return new MoveRandomlyTask(this);
      }
    }
  }

  public int Modify(int input) {
    return input - 1;
  }
}
