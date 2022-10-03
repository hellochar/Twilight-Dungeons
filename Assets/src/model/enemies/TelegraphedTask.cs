using System;
using System.Collections.Generic;

[System.Serializable]
public class TelegraphedTask : ActorTask {
  public override TaskStage WhenToCheckIsDone => TaskStage.After;
  private int turns;
  private readonly ActionType type;
  // temp
  public BaseAction then;
  private bool done;

  public TelegraphedTask(Actor actor, int turns, BaseAction then, ActionType type) : base(actor) {
    this.turns = turns;
    this.type = type;
    this.then = then;
  }
  public TelegraphedTask(Actor actor, int turns, BaseAction then) : this(actor, turns, then, then.Type) {}

  protected override BaseAction GetNextActionImpl() {
    // 3, 2, 1
    if (turns > 0) {
      turns--;
      return new WaitBaseAction(actor, type);
    }
    done = true;
    return then;
  }

  public override bool IsDone() => done;
}
