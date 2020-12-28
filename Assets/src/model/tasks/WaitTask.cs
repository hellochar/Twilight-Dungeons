using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Close-ended.</summary>
public class WaitTask : ActorTask {
  private int turns;

  public int Turns => turns;

  public WaitTask(Actor actor, int turns) : base(actor) {
    this.turns = turns;
  }

  public override IEnumerator<BaseAction> Enumerator() {
    do {
      turns--;
      yield return new WaitBaseAction(actor);
    } while (turns > 0);
  }

  public override bool IsDone() => turns <= 0;
}