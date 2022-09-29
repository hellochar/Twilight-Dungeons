using System;
using UnityEngine;

[Serializable]
[ObjectInfo("nubs", "Plant at home to produce water.\nIn the caves, it is destroyed when any creature walks off it.")]
public class Nubs : Grass, IActorLeaveHandler {
  public Nubs(Vector2Int pos) : base(pos) {
  }

  public void HandleActorLeave(Actor who) {
    if (floor.depth != 0) {
      Kill(who);
    }
  }

  public override void StepDay() {
    GameModel.main.player.water += 2;
    base.StepDay();
  }
}
