using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public abstract class ActorAction {
  public virtual Actor actor { get; }
  public event Action OnDone;
  private bool hasPerformedOnce = false;

  protected ActorAction(Actor actor) { this.actor = actor; }

  /// return the number of ticks it took to perform this action
  public virtual int Perform() {
    hasPerformedOnce = true;
    return actor.baseActionCost;
  }

  public virtual bool IsDone() {
    return hasPerformedOnce;
  }

  internal void Finish() {
    OnDone?.Invoke();
  }
}

public class FollowPathAction : ActorAction {
  public Vector2Int target { get; }
  public readonly List<Vector2Int> path;
  public FollowPathAction(Actor actor, Vector2Int target, List<Vector2Int> path) : base(actor) {
    this.target = target;
    this.path = path;
  }

  public override int Perform() {
    if (path.Count > 0) {
      Vector2Int nextPosition = path[0];
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

public class MoveToTargetAction : FollowPathAction {
  public MoveToTargetAction(Actor actor, Vector2Int target) : base(actor, target, GameModel.main.currentFloor.FindPath(actor.pos, target)) {
  }
}

public class MoveNextToTargetAction : FollowPathAction {
  public MoveNextToTargetAction(Actor actor, Vector2Int target) : base(actor, target, FindBestAdjacentPath(actor.pos, target)) { }

  private static List<Vector2Int> FindBestAdjacentPath(Vector2Int pos, Vector2Int target) {
    /// TODO optimize: https://zach.se/a-star-search-with-multiple-targets-and-sources/
    var adjacent = new List<Vector2Int>();
    adjacent.Add(target + new Vector2Int(-1, -1));
    adjacent.Add(target + new Vector2Int(-1, 0));
    adjacent.Add(target + new Vector2Int(-1, 1));

    adjacent.Add(target + new Vector2Int(0, -1));
    // adjacent.Add(target + new Vector2Int(0, 0));
    adjacent.Add(target + new Vector2Int(0, 1));

    adjacent.Add(target + new Vector2Int(1, -1));
    adjacent.Add(target + new Vector2Int(1, 0));
    adjacent.Add(target + new Vector2Int(1, 1));

    adjacent.Sort((a, b) => Math.Sign(Vector2Int.Distance(pos, a) - Vector2Int.Distance(pos, b)));
    var paths = adjacent.Select(x => GameModel.main.currentFloor.FindPath(pos, x)).Where(list => list.Count > 0);
    return paths.FirstOrDefault() ?? new List<Vector2Int>();
  }
}

public class GenericAction : ActorAction {
  public GenericAction(Actor actor, Action action) : base(actor) {
    Action = action;
  }

  public Action Action { get; }

  public override int Perform() {
    Action.Invoke();
    return base.Perform();
  }
}