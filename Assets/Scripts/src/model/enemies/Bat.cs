using System.Collections.Generic;
using UnityEngine;

public class Bat : AIActor {
  public Bat(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    ai = AIs.BatAI(this).GetEnumerator();
  }
}
