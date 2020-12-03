using System;
using System.Collections.Generic;
using UnityEngine;

public class WaitAction : ActorAction {
  private int turns;

  public WaitAction(Actor actor, int turns) : base(actor) {
    this.turns = turns;
  }

  public override IEnumerator<BaseAction> Enumerator() {
    for (var i = 0; i < turns; i++) {
      yield return new WaitBaseAction(actor);
    }
  }
}