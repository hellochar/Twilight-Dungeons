using System;
using UnityEngine;

[System.Serializable]
public class MoveToTargetTask : FollowPathTask {
  public MoveToTargetTask(Actor actor, Vector2Int target, Floor floor = null) : base(actor, target, (floor ?? GameModel.main.currentFloor).FindPath(actor.pos, target, false, actor)) {
  }
}

public class MoveToTargetThenPerformTask : MoveToTargetTask {
  private readonly Action then;

  public MoveToTargetThenPerformTask(Actor actor, Vector2Int target, Action then, Floor floor = null) : base(actor, target, floor) {
    this.then = then;
  }

  public override void PostStep(BaseAction action, BaseAction finalAction) {
    if (IsDone()) {
      then();
    }
  }
}
