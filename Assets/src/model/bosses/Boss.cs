using System;
using UnityEngine;

[Serializable]
public abstract class Boss : AIActor, IActionPerformedHandler {
  public bool isSeen = false;

  protected Boss(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
   }

  protected virtual void OnSeen() {}

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (isVisible && !isSeen) {
      OnSeen();
      isSeen = true;
      GameModel.main.OnBossSeen(this);
    }
  }

  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);
    floor.Put(new HeartTrigger(pos));
  }

}
