using System;
using System.Collections.Generic;

public class TelegraphedTask : ActorTask {
  private int turns;
  private BaseAction then;
  private bool done;

  public TelegraphedTask(Actor actor, int turns, BaseAction then) : base(actor) {
    this.turns = turns;
    this.then = then;
  }

  public override IEnumerator<BaseAction> Enumerator() {
    for (int i = 0; i < turns; i++) {
      yield return new WaitBaseAction(actor);
    }
    done = true;
    yield return then;
  }

  public override bool IsDone() => done;
}
