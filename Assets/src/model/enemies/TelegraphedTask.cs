using System;
using System.Collections.Generic;

[System.Serializable]
public class TelegraphedTask : ActorTask {
  public override TaskStage WhenToCheckIsDone => TaskStage.After;
  private int turns;
  private BaseAction then;
  private bool done;

  public TelegraphedTask(Actor actor, int turns, BaseAction then) : base(actor) {
    this.turns = turns;
    this.then = then;
  }

  protected override BaseAction GetNextActionImpl() {
    // 3, 2, 1
    if (turns > 0) {
      turns--;
      return new WaitBaseAction(actor);
    }
    done = true;
    return then;
  }

  public override bool IsDone() => done;
}
