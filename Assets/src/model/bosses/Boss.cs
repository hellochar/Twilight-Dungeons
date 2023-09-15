using System;
using UnityEngine;

[Serializable]
public abstract class Boss : AIActor {
  public bool isSeen = false;

  protected Boss(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
   }

  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);
    floor.Put(new HeartTrigger(pos));
  }
}
