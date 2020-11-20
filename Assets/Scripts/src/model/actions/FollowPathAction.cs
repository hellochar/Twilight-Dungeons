using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FollowPathAction : ActorAction {
  public Vector2Int target { get; }
  public List<Vector2Int> path;
  public FollowPathAction(Actor actor, Vector2Int target, List<Vector2Int> path) : base(actor) {
    this.target = target;
    this.path = path;
  }

  public override int Perform() {
    if (path.Any()) {
      Vector2Int nextPosition = path.First();
      path.RemoveAt(0);
      actor.pos = nextPosition;
    }
    return base.Perform();
  }

  public override bool IsDone() {
    return path.Count == 0;
    // return actor.pos == target;
  }
}
