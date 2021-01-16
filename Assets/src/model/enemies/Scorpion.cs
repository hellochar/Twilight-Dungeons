using System;
using UnityEngine;

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
    ai = AIs.JackalAI(this).GetEnumerator();
  }

  internal override (int, int) BaseAttackDamage() => (1, 1);
}
