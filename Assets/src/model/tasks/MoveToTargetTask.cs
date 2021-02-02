using UnityEngine;

[System.Serializable]
public class MoveToTargetTask : FollowPathTask {
  public MoveToTargetTask(Actor actor, Vector2Int target, Floor floor = null) : base(actor, target, (floor ?? GameModel.main.currentFloor).FindPath(actor.pos, target)) {
  }
}
