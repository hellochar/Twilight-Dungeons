using System;
using UnityEngine;

[Serializable]
class HeartTrigger : Trigger {
  public HeartTrigger(Vector2Int pos) : base(pos, null) {
  }

  public override void HandleActorEnter(Actor who) {
    if (who is Player player) {
      player.AddMaxHP(4);
      player.Replenish();
      SpriteFlyAnimation.Create(MasterSpriteAtlas.atlas.GetSprite("heart_animated_2_0"), Util.withZ(pos), GameObject.Find("Hearts"));
      KillSelf();
    }
  }
}

// public class SporeColonyBoss : AIActor {
//   public SporeColonyBoss(Vector2Int pos) : base(pos) {
//     hp = baseMaxHp = 
//     faction = Faction.Enemy;
//   }

//   protected override ActorTask GetNextTask() {
//     throw new NotImplementedException();
//   }
// }
