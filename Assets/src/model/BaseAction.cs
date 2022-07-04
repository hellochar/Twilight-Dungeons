
using System;
using UnityEngine;
/// BaseActions always last exactly one turn
[System.Serializable]
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

[System.Serializable]
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

[System.Serializable]
public sealed class AttackBaseAction : BaseAction {
  public override ActionType Type => ActionType.ATTACK;
  public readonly Body target;

  public AttackBaseAction(Actor actor, Body target) : base(actor) {
    Debug.Assert(target != null, "attacking a null target");
    this.target = target;
  }

  public override void Perform() {
    if (actor.IsNextTo(target)) {
      actor.Attack(target);
    } else {
      throw new CannotPerformActionException("Cannot reach target!");
    }
  }
}

[System.Serializable]
public sealed class AttackGroundBaseAction : BaseAction {
  public override ActionType Type => ActionType.ATTACK;
  public readonly Vector2Int targetPosition;

  public AttackGroundBaseAction(Actor actor, Vector2Int targetPosition) : base(actor) {
    this.targetPosition = targetPosition;
  }

  public override void Perform() {
    actor.AttackGround(targetPosition);
  }
}

[System.Serializable]
public sealed class WaitBaseAction : BaseAction {
  public override ActionType Type { get; }

  public WaitBaseAction(Actor actor, ActionType type = ActionType.WAIT) : base(actor) {
    Type = type;
  }

  public override void Perform() {}
}

[System.Serializable]
public sealed class GenericBaseAction : BaseAction {
  private ActionType m_type;
  public override ActionType Type => m_type;
  public readonly Action action;
  public GenericBaseAction(Actor actor, Action action, ActionType type = ActionType.GENERIC) : base(actor) {
    m_type = type;
    this.action = action;
  }

  public override void Perform() {
    action();
  }
}

[System.Serializable]
public class StruggleBaseAction : BaseAction {
  public override ActionType Type => ActionType.MOVE;
  public StruggleBaseAction(Actor actor) : base(actor) {
  }

  public override void Perform() {
    // no op
  }
}