
using System;
using System.Collections.Generic;
/// Don't do anything until the Player's in view
class SleepTask : ActorTask, IDamageTakenModifier {
  private bool done;
  private int? maxTurns;
  private readonly bool isDeepSleep;

  public bool wakeUpNextTurn { get; protected set; }
  public SleepTask(Actor actor, int? maxTurns = null, bool isDeepSleep = false) : base(actor) {
    actor.OnTakeDamage += HandleTakeDamage;
    this.maxTurns = maxTurns;
    this.isDeepSleep = isDeepSleep;
  }

  private void HandleTakeDamage(int dmg, int hp, Actor source) {
    wakeUpNextTurn = true;
  }

  internal override void Ended() {
    actor.OnTakeDamage -= HandleTakeDamage;
  }

  protected virtual bool ShouldWakeUp() {
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
      if (wakeUpNextTurn) {
        break;
      }
      yield return new WaitBaseAction(actor);
    }
    // end of sleep - wake up adjacent sleeping Actors
    foreach (var actor in actor.floor.AdjacentActors(actor.pos)) {
      if (actor.task is SleepTask s) {
        // hack to wake them up
        s.wakeUpNextTurn = true;
      }
    }
    done = true;
    GameModel.main.EnqueueEvent(() => {
      actor.statuses.Add(new SurprisedStatus());
    });
    yield return new WaitBaseAction(actor);
  }

  /// doubles damage taken while sleeping!
  public int Modify(int input) {
    return input * 2;
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

  public override void Stack(Status other) {}
}
