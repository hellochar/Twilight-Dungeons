
using System;
using UnityEngine;
/// BaseActions always last exactly one turn
public abstract class BaseAction {
  public readonly Actor actor;
  public abstract ActionType Type { get; }

  protected BaseAction(Actor actor) {
    this.actor = actor;
  }

  public abstract void Perform();
}


public enum ActionType {
  MOVE,
  ATTACK,
  WAIT,
  GENERIC
}

public sealed class MoveBaseAction : BaseAction {
  public override ActionType Type => ActionType.MOVE;
  public readonly Vector2Int pos;

  public MoveBaseAction(Actor actor, Vector2Int pos) : base(actor) {
    this.pos = pos;
  }

  /// returns true if it worked, false if not
  public override void Perform() {
    if (!actor.IsNextTo(pos)) {
      return;
    }
    actor.pos = pos;
  }
}

public sealed class AttackBaseAction : BaseAction {
  public override ActionType Type => ActionType.ATTACK;
  public readonly Actor target;

  public AttackBaseAction(Actor actor, Actor target) : base(actor) {
    this.target = target;
  }

  public override void Perform() {
    if (actor.IsNextTo(target)) {
      actor.Attack(target);
    }
  }
}

public sealed class AttackGroundBaseAction : BaseAction {
  public override ActionType Type => ActionType.ATTACK;
  public readonly Vector2Int targetPosition;

  public AttackGroundBaseAction(Actor actor, Vector2Int targetPosition) : base(actor) {
    this.targetPosition = targetPosition;
  }

  public override void Perform() {
    if (actor.IsNextTo(targetPosition)) {
      actor.AttackGround(targetPosition);
    }
  }
}

public sealed class WaitBaseAction : BaseAction {
  public override ActionType Type => ActionType.WAIT;

  public WaitBaseAction(Actor actor) : base(actor) {
  }

  public override void Perform() {}
}

public sealed class GenericBaseAction : BaseAction {
  public override ActionType Type => ActionType.GENERIC;
  public readonly Action<Actor> action;
  public GenericBaseAction(Actor actor, Action<Actor> action) : base(actor) {
    this.action = action;
  }

  public override void Perform() {
    action.Invoke(actor);
  }
}

public class StruggleBaseAction : BaseAction {
  public override ActionType Type => ActionType.MOVE;
  public StruggleBaseAction(Actor actor) : base(actor) {
  }

  public override void Perform() {
    var stuckStatus = actor.statuses.FindOfType<StuckStatus>();
    if (stuckStatus != null) {
      actor.statuses.Remove(stuckStatus);
    }
  }
}