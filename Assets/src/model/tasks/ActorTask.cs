using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum TaskStage {
  /// <summary>Check right before the actor's turn, so that this task might
  /// chain into another task.</summary>
  Before = 1,
  /// <summary> Check right after this task's action was just performed, so
  /// that the player knows what the creature's next task is.</summary>
  After = 2
}

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
[Serializable]
public abstract class ActorTask {
  public string Name { get; set; }
  public Actor actor { get; }
  /// when to check whether this task is "done":
  public virtual TaskStage WhenToCheckIsDone => TaskStage.Before;

  protected ActorTask(Actor actor) {
    Name = Util.WithSpaces(GetType().Name.Replace("Task", ""));
    this.actor = actor;
  }

  public ActorTask Named(string name) {
    Name = name;
    return this;
  }

  /// called before the IsDone()/GetNextAction() calls
  public virtual void PreStep() {}

  public virtual BaseAction GetNextAction() {
    return GetNextActionImpl();
  }

  public abstract bool IsDone();

  protected abstract BaseAction GetNextActionImpl();

  /// <summary>Called by Actor when this task has ended.</summary>
  internal virtual void Ended() { }
}

[System.Serializable]
/// A close-ended action that, once it has been stepped once, is done.
public abstract class DoOnceTask : ActorTask {
  public override TaskStage WhenToCheckIsDone => TaskStage.After;
  private bool hasDoneOnce = false;
  protected DoOnceTask(Actor actor) : base(actor) { }

  public sealed override BaseAction GetNextAction() {
    hasDoneOnce = true;
    return base.GetNextAction();
  }

  public override sealed bool IsDone() => hasDoneOnce;
}