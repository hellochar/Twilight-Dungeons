using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Bat : AIActor {
  bool isHungry = false;
  public Bat(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 5;
    faction = Faction.Enemy;
    ai = AI().GetEnumerator();
    OnDealDamage += HandleDealDamage;
    OnTakeDamage += HandleTakeDamage;
  }

  /// bats hide in corners and occasionally attack the closest target
  private IEnumerable<ActorTask> AI() {
    yield return new SleepTask(actor);
    while (true) {
      if (isHungry) {
        var potentialTargets = actor.floor
          .AdjacentActors(actor.pos)
          .Where((t) => actor.floor.TestVisibility(actor.pos, t.pos) && !(t is Bat));
        if (potentialTargets.Any()) {
          var target = Util.RandomPick(potentialTargets);
          yield return new AttackTask(actor, target);
        } else {
          yield return new MoveRandomlyTask(actor);
        }
      } else {
        yield return new DeepSleepTask(actor, 10);
        isHungry = true;
      }
    }
  }


  private void HandleTakeDamage(int dmg, int hp, Actor source) {
    SetTasks(new AttackTask(this, source));
  }

  private void HandleDealDamage(int dmg, Actor target) {
    if (dmg > 0) {
      Heal(1);
      isHungry = false;
    }
  }

  internal override int BaseAttackDamage() {
    return 1;
  }
}
