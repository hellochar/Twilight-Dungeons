using System;
using UnityEngine;
using UnityEngine.Events;

public abstract class ActorAction {
  public virtual Actor actor { get; }
  public event Action OnDone;
  protected bool hasPerformedOnce = false;

  protected ActorAction(Actor actor) { this.actor = actor; }

  public virtual string displayName => Util.WithSpaces(GetType().Name.Replace("Action", ""));

  /// return the number of ticks it took to perform this action
  public virtual void Perform() {
    hasPerformedOnce = true;
  }

  public virtual bool IsDone() {
    return hasPerformedOnce;
  }

  internal void Finish() {
    OnDone?.Invoke();
  }
}
