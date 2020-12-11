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
public abstract class ActorTask : IEnumerator<BaseAction> {
  public string Name { get; set; }
  public virtual Actor actor { get; }

  /// default implementation is not done (aka open-ended).
  /// Note: IsDone() should return true if .MoveNext() would
  /// return false.
  public virtual bool IsDone() => false;

  protected ActorTask(Actor actor) {
    Name = Util.WithSpaces(GetType().Name.Replace("Task", ""));
    this.actor = actor;
  }

  public ActorTask Named(string name) {
    Name = name;
    return this;
  }

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
public abstract class DoOnceTask : ActorTask {
  private bool hasDoneOnce = false;

  protected DoOnceTask(Actor actor) : base(actor) { }

  public override bool MoveNext() {
    hasDoneOnce = true;
    return base.MoveNext();
  }

  public override sealed bool IsDone() => hasDoneOnce;
}