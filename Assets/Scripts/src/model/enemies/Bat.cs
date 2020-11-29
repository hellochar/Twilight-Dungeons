using System;
using System.Collections.Generic;
using UnityEngine;

public class Bat : AIActor {
  public Bat(Vector2Int pos) : base(pos) {
    hp = hpMax = 5;
    faction = Faction.Enemy;
    ai = AIs.BatAI(this).GetEnumerator();
    OnAttack += HandleAttack;
    OnTakeDamage += HandleTakeDamage;
  }

  private void HandleTakeDamage(int dmg, int hp, Actor source) {
    SetActions(
      new ChaseTargetAction(this, source),
      new AttackAction(this, source),
      new ChaseTargetAction(this, source),
      new AttackAction(this, source),
      new ChaseTargetAction(this, source),
      new AttackAction(this, source)
    );
  }

  private void HandleAttack(int dmg, Actor target) {
    if (dmg > 0) {
      Heal(1);
    }
  }

  internal override int GetAttackDamage() {
    return 1;
  }
}
