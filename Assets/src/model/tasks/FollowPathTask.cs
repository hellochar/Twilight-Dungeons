using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// Close-ended.
public class FollowPathTask : ActorTask {
  public Vector2Int target { get; }
  public List<Vector2Int> path;
  public FollowPathTask(Actor actor, Vector2Int target, List<Vector2Int> path) : base(actor) {
    this.target = target;
    this.path = path;
  }

  public override IEnumerator<BaseAction> Enumerator() {
    while (path.Any()) {
      Vector2Int nextPosition = path.First();
      path.RemoveAt(0);
      /// TODO cancel this action if MoveBaseAction failed
      yield return new MoveBaseAction(actor, nextPosition);
    }
  }

  public virtual void OnGetNextPosition() {}

  public override bool IsDone() => path.Count == 0;
}
