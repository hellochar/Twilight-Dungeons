using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * A task comprised of zero or more individual actions that an 
 * actor takes to help reach some goal.
 *
 * Actions are lazily evaluated streams of BaseActions that are "open-ended"
 * by default, in that they may never end. A "chase target" action could
 * theoretically last forever, if you never reach the target. The evaluation
 * depends on mutable state of the world (e.g. player's position), so the decision
 * must happen right when the Actor is about to take an action. This means Actions
 * only Ends right before the Actor must take a new Action.
 * 
 * But some actions are "close-ended" - they have a defined end that is guaranteed to
 * terminate. In order to accomodate close-ended actions, we define the IsDone() method
 * that gets called right after the generated BaseAction has been taken. If IsDone()
 * is true, then the action Ends immediately.
 */
public abstract class ActorAction : IEnumerator<BaseAction> {
  public virtual string displayName => Util.WithSpaces(GetType().Name.Replace("Action", ""));
  public virtual Actor actor { get; }
  public event Action OnDone;

  internal void Finish() {
    OnDone?.Invoke();
  }

  /// default implementation is not done (aka open-ended).
  /// Note: IsDone() should return true if .MoveNext() would
  /// return false.
  public virtual bool IsDone() => false;

  protected ActorAction(Actor actor) { this.actor = actor; }

  public abstract IEnumerator<BaseAction> Enumerator();

  private IEnumerator<BaseAction> _enumeratorInstance;
  private IEnumerator<BaseAction> EnumeratorInstance {
    get {
      if (_enumeratorInstance == null) {
        _enumeratorInstance = Enumerator();
      }
      return _enumeratorInstance;
    }
  }

  public BaseAction Current => EnumeratorInstance.Current;

  object IEnumerator.Current => EnumeratorInstance.Current;

  public virtual bool MoveNext() {
    return EnumeratorInstance.MoveNext();
  }

  public void Reset() {
    EnumeratorInstance.Reset();
  }

  public void Dispose() {
    EnumeratorInstance.Dispose();
  }
}

/// A close-ended action that, once it has been stepped once, is done.
public abstract class DoOnceActorAction : ActorAction {
  private bool hasDoneOnce = false;

  protected DoOnceActorAction(Actor actor) : base(actor) {}

  public override bool MoveNext() {
    hasDoneOnce = true;
    return base.MoveNext();
  }

  public override sealed bool IsDone() => hasDoneOnce;
}

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