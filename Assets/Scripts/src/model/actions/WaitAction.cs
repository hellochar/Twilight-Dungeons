using System;
using System.Collections.Generic;
using UnityEngine;

public class WaitAction : ActorAction {
  private int turns;

  public WaitAction(Actor actor, int turns) : base(actor) {
    this.turns = turns;
  }

  public override void Perform() {
    turns--;
  }

  public override bool IsDone() {
    return turns <= 0;
  }
}