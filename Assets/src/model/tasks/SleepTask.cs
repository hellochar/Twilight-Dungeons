
using System;
using System.Collections.Generic;
using System.Linq;
/// Don't do anything until the Player's in view
[System.Serializable]
class SleepTask : ActorTask, IAttackDamageTakenModifier, ITakeAnyDamageHandler {
  public override TaskStage WhenToCheckIsDone => TaskStage.After;
  private bool done = false;
  private int? maxTurns;
  public readonly bool isDeepSleep;

  public bool wakeUpNextTurn { get; set; }
  public SleepTask(Actor actor, int? maxTurns = null, bool isDeepSleep = false) : base(actor) {
    this.maxTurns = maxTurns;
    this.isDeepSleep = isDeepSleep;
  }

  /// doubles attack damage taken while sleeping
  public int Modify(int input) {
    return input * 2;
  }

  /// wake up when hurt!
  public void HandleTakeAnyDamage(int damage) {
    if (damage > 0) {
      // Wake up early by ending this task immediately (will trigger Ended())
      actor.statuses.Add(new SurprisedStatus());
      actor.GoToNextTask();
    }
  }

  protected virtual bool ShouldWakeUp() {
    if (wakeUpNextTurn) {
      return true;
    }

    if (maxTurns != null && maxTurns <= 0) {
      return true;
    }

    if (isDeepSleep) {
      return false;
    }

    return actor.isVisible && actor.CanTargetPlayer();
  }

  protected override BaseAction GetNextActionImpl() {
    if (maxTurns != null) {
      maxTurns--;
    }
    if (ShouldWakeUp()) {
      // // end of sleep - wake up adjacent sleeping Actors
      // foreach (var actor in actor.floor.AdjacentActors(actor.pos)) {
      //   if (actor.task is SleepTask s && !s.isDeepSleep) {
      //     // hack to wake them up
      //     s.wakeUpNextTurn = true;
      //   }
      // }
      done = true;
      actor.statuses.Add(new SurprisedStatus());
    }
    return new WaitBaseAction(actor);
  }

  public override bool IsDone() => done;
}

[System.Serializable]
[ObjectInfo("colored_transparent_packed_658", description: "You're surprised! You must spend the next turn shaking it off.")]
class SurprisedStatus : Status, IBaseActionModifier {
  public override bool isDebuff => true;
  public override string Info() => "";
  private bool remove = false;

  public BaseAction Modify(BaseAction input) {
    /// The SurprisedStatus only affects one action but it ends at the start of the second action,
    /// so the player can see the (!)
    if (remove) {
      Remove();
      return input;
    } else {
      // treat player differently - remove status immediately, and do a struggle
      if (actor == GameModel.main.player) {
        Remove();
        return new StruggleBaseAction(actor);
      } else {
        remove = true;
        return new WaitBaseAction(input.actor);
      }
    }
  }

  public override bool Consume(Status other) => true;
}
