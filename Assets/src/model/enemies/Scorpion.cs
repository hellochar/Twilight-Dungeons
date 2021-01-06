using System;
using UnityEngine;

public class Scorpion : AIActor {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.MOVE] = 0.5f,
    [ActionType.ATTACK] = 0.5f,
  };

  protected override ActionCosts actionCosts => StaticActionCosts;
  public Scorpion(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 5;
    ai = AIs.JackalAI(this).GetEnumerator();
  }

  internal override int BaseAttackDamage() {
    return 1;
  }
}
