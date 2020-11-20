using UnityEngine;

public class MoveToTargetAction : FollowPathAction {
  public MoveToTargetAction(Actor actor, Vector2Int target) : base(actor, target, GameModel.main.currentFloor.FindPath(actor.pos, target)) {
  }
}
