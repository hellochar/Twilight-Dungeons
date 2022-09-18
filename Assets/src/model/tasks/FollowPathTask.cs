using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class FollowPathTask : ActorTask {
  public override TaskStage WhenToCheckIsDone => TaskStage.After;
  public Vector2Int target { get; }
  public List<Vector2Int> path;
  public int maxMoves = int.MaxValue;
  public int timesMoved { get; private set; }
  public FollowPathTask(Actor actor, Vector2Int target, List<Vector2Int> path) : base(actor) {
    this.target = target;
    this.path = path;
  }

  protected override BaseAction GetNextActionImpl() {
    Vector2Int nextPosition = path.First();
    path.RemoveAt(0);
    timesMoved++;
    return new MoveBaseAction(actor, nextPosition);
  }

  public override bool IsDone() => path.Count == 0 || timesMoved >= maxMoves;
}
