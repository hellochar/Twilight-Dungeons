using System;
using UnityEngine;

[Serializable]
[ObjectInfo("bloodwort", description: "If the Player is attacked while standing over the Bloodwort, they get 4 stacks of strength and the Bloodwort dies.")]
public class Bloodwort : Grass, IBodyTakeAttackDamageHandler {
  public Bloodwort(Vector2Int pos) : base(pos) {
    BodyModifier = this;
  }

  public void HandleTakeAttackDamage(int damage, int hp, Actor source) {
    if (body is Player p) {
      p.statuses.Add(new StrengthStatus(4));
      KillSelf();
    }
  }
}