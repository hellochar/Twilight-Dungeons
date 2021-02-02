using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Close-ended.</summary>
[System.Serializable]
public class WaitTask : ActorTask {
  public override TaskStage WhenToCheckIsDone => TaskStage.After;
  private int turns;

  public int Turns => turns;

  public WaitTask(Actor actor, int turns) : base(actor) {
    this.turns = turns;
  }

  protected override BaseAction GetNextActionImpl() {
    turns--;
    return new WaitBaseAction(actor);
  }

  public override bool IsDone() => turns <= 0;
}