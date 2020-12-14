using System;
using System.Collections.Generic;
using UnityEngine;

public class WaitTask : ActorTask {
  private int turns;

  public int Turns => turns;

  public WaitTask(Actor actor, int turns) : base(actor) {
    this.turns = turns;
  }

  public override IEnumerator<BaseAction> Enumerator() {
    for (; turns > 0; turns--) {
      yield return new WaitBaseAction(actor);
    }
  }
}