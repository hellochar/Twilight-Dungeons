
using System;
using System.Collections.Generic;
using System.Linq;
/// Don't do anything until the Player's in view
class SleepTask : ActorTask, IAnyDamageTakenModifier {
  private bool done;
  private int? maxTurns;
  private readonly bool isDeepSleep;

  public bool wakeUpNextTurn { get; protected set; }
  public SleepTask(Actor actor, int? maxTurns = null, bool isDeepSleep = false) : base(actor) {
    this.maxTurns = maxTurns;
    this.isDeepSleep = isDeepSleep;
  }

  /// doubles damage taken while sleeping, also should wake up!
  public int Modify(int input) {
    if (input > 0) {
      WakeUp();
    }
    return input * 2;
  }

  protected virtual bool ShouldWakeUp() {
    if (wakeUpNextTurn) {
      return true;
    }

    if (isDeepSleep) {
      return false;
    }

    var chanceToWakeUpWhileVisible = 1;
    return actor.isVisible && UnityEngine.Random.value < chanceToWakeUpWhileVisible;
  }

  public override IEnumerator<BaseAction> Enumerator() {
    while (!ShouldWakeUp()) {
      if (maxTurns != null) {
        maxTurns--;
        if (maxTurns <= 0) {
          break;
        }
      }
      yield return new WaitBaseAction(actor);
    }
    // end of sleep - wake up adjacent sleeping Actors
    foreach (var actor in actor.floor.AdjacentActors(actor.pos)) {
      if (actor.task is SleepTask s && !s.isDeepSleep) {
        // hack to wake them up
        s.wakeUpNextTurn = true;
      }
    }
    WakeUp();
    yield return new WaitBaseAction(actor);
  }

  public void WakeUp() {
    if (!done) {
      done = true;
      GameModel.main.EnqueueEvent(() => {
        actor.statuses.Add(new SurprisedStatus());
      });
      wakeUpNextTurn = true;
    }
  }

  public override bool IsDone() => done;
}

class SurprisedStatus : Status, IBaseActionModifier {
  public override bool isDebuff => true;
  public override string Info() => "";

  public BaseAction Modify(BaseAction input) {
    Remove();
    return input;
  }

  public override bool Consume(Status other) => true;
}
