using System;
using System.Collections.Generic;
using UnityEngine;

public class Bat : AIActor {
  public Bat(Vector2Int pos) : base(pos) {
    hp = hpMax = 5;
    faction = Faction.Enemy;
    ai = AIs.BatAI(this).GetEnumerator();
    OnDealDamage += HandleDealDamage;
  }

  private void HandleDealDamage(int dmg, Actor target) {
    if (dmg > 0) {
      Heal(1);
    }
  }

  internal override int BaseAttackDamage() {
    return 1;
  }
}
