using System;
using System.Collections.Generic;
using UnityEngine;

public class Blob : AIActor {
  public Blob(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 8;
    faction = Faction.Enemy;
    ai = AIs.BlobAI(this).GetEnumerator();
  }

  internal override int BaseAttackDamage() {
    return UnityEngine.Random.Range(2, 4);
  }
}
