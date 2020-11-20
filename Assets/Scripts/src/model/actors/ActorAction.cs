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

  public virtual string displayName => Util.WithSpaces(GetType().Name.Replace("Action", ""));

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

public class MoveToTargetAction : FollowPathAction {
  public MoveToTargetAction(Actor actor, Vector2Int target) : base(actor, target, GameModel.main.currentFloor.FindPath(actor.pos, target)) {
  }
}

public class MoveNextToTargetAction : FollowPathAction {
  public MoveNextToTargetAction(Actor actor, Vector2Int target) : base(actor, target, FindBestAdjacentPath(actor.pos, target)) { }

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

public class ChaseTargetAction : MoveNextToTargetAction {
  private readonly Actor targetActor;

  public ChaseTargetAction(Actor actor, Actor targetActor) : base(actor, targetActor.pos) {
    this.targetActor = targetActor;
  }

  public override int Perform() {
    /// recompute the path
    this.path = FindBestAdjacentPath(actor.pos, targetActor.pos);
    return base.Perform();
  }
}

public class GenericAction : ActorAction {
  public GenericAction(Actor actor, Action<Actor> action) : base(actor) {
    Action = action;
  }

  public Action<Actor> Action { get; }

  public override int Perform() {
    Action.Invoke(actor);
    return base.Perform();
  }

  public override string displayName => Util.WithSpaces(Action.Method.Name);
}