using UnityEngine;

public class MoveToTargetTask : FollowPathTask {
  public MoveToTargetTask(Actor actor, Vector2Int target) : base(actor, target, GameModel.main.currentFloor.FindPath(actor.pos, target)) {
  }
}
