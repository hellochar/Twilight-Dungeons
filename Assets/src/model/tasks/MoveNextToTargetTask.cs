using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class MoveNextToTargetTask : FollowPathTask {
  public MoveNextToTargetTask(Actor actor, Vector2Int target) : base(actor, target, FindBestAdjacentPath(actor.pos, target)) { }

  public static List<Vector2Int> FindBestAdjacentPath(Vector2Int pos, Vector2Int target) {
    if (pos == target) {
      return new List<Vector2Int>();
    }
    var path = GameModel.main.currentFloor.FindPath(pos, target, true);
    if (path.Any()) {
      path.RemoveAt(path.Count - 1);
    }
    return path;
  }
}
