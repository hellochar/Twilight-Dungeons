using System.Collections.Generic;
using UnityEngine;

public abstract class ActorAction {
  public virtual Actor actor { get; }

  protected ActorAction(Actor actor) { this.actor = actor; }

  /// return the number of ticks it took to perform this action
  public abstract int Perform();

  public virtual bool IsDone() {
    return true;
  }
}

public class TeleportAction : ActorAction {
  Vector2Int target;
  public TeleportAction(Actor actor, Vector2Int target) : base(actor) {
    this.target = target;
  }

  public override int Perform() {
    actor.pos = target;
    return actor.baseActionCost;
  }
}

public class MoveToTargetAction : ActorAction {
  public Vector2Int target { get; }
  public readonly List<Vector2Int> path;

  public MoveToTargetAction(Actor actor, Vector2Int target) : base(actor) {
    this.target = target;
    Floor floor = GameModel.main.currentFloor;
    this.path = floor.FindPath(actor.pos, target);
  }

  public override int Perform() {
    if (path.Count > 0) {
      Vector2Int nextPosition = path[0];
      path.RemoveAt(0);
      actor.pos = nextPosition;
    }
    return actor.baseActionCost;
  }

  public override bool IsDone() {
    return path.Count == 0;
    // return actor.pos == target;
  }
}
