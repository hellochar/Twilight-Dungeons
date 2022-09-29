using System;
using UnityEngine;

[Serializable]
[ObjectInfo("soft-moss", "Plant at home to turn into Soil.\nIn the caves, it is destroyed when any creature walks off it.")]
public class SoftMoss : Grass, IActorLeaveHandler {
  public SoftMoss(Vector2Int pos) : base(pos) {
  }

  public void HandleActorLeave(Actor who) {
    if (floor.depth != 0) {
      Kill(who);
    }
  }

  public override void StepDay() {
    if (!(tile is Soil)) {
      floor.Put(new Soil(pos));
      KillSelf();
    } else {
      base.StepDay();
    }
  }
}
