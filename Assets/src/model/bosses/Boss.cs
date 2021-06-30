using System;
using UnityEngine;

[Serializable]
public abstract class Boss : AIActor {
  public bool isSeen = false;

  internal virtual bool EnsureSeen() {
    if (!isSeen) {
      OnSeen();
      isSeen = true;
      return true;
    }
    return false;
  }

  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);
    floor.Put(new HeartTrigger(pos));
  }

  protected Boss(Vector2Int pos) : base(pos) { }

  protected virtual void OnSeen() {}
}
