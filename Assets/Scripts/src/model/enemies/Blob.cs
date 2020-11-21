using System.Collections.Generic;
using UnityEngine;

public class Blob : AIActor {
  public Blob(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    ai = AIs.BlobAI(this).GetEnumerator();
  }
}
