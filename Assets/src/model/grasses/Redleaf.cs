using System;
using UnityEngine;

[Serializable]
[ObjectInfo("redleaf", "Produces healing when planted at home.")]
public class Redleaf : Grass {
  public Redleaf(Vector2Int pos) : base(pos) {
  }

  public override void StepDay() {
    floor.Put(new ItemOnGround(pos, new ItemRedleaf(), pos));
    KillSelf();
    if (!IsDead) {
      base.StepDay();
    }
  }
}

[System.Serializable]
[ObjectInfo("redleaf")]
class ItemRedleaf : Item, IEdible {
  public void Eat(Actor a) {
    a.Heal(1);
    Destroy();
  }

  internal override string GetStats() => "Heals 1 HP.";
}
