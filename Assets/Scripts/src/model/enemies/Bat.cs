using System;
using System.Collections.Generic;
using UnityEngine;

public class Bat : AIActor {
  public Bat(Vector2Int pos) : base(pos) {
    hp = hpMax = 5;
    faction = Faction.Enemy;
    ai = AIs.BatAI(this).GetEnumerator();
    OnAttack += HandleAttack;
  }

  private void HandleAttack(int dmg, Actor target) {
    Heal(1);
  }

  internal override int GetAttackDamage() {
    return UnityEngine.Random.Range(1, 3);
  }
}
