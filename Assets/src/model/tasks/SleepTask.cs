
using System;
using System.Collections.Generic;
/// Don't do anything until the Player's in view
class SleepTask : ActorTask, IDamageTakenModifier {
  public bool wakeUpNextTurn { get; protected set; }
  public SleepTask(Actor actor) : base(actor) {
    actor.OnTakeDamage += HandleTakeDamage;
  }

  private void HandleTakeDamage(int dmg, int hp, Actor source) {
    wakeUpNextTurn = true;
  }

  internal override void Ended() {
    actor.OnTakeDamage -= HandleTakeDamage;
  }

  public override IEnumerator<BaseAction> Enumerator() {
    var chanceToWakeUpWhileVisible = 0.25;
    bool ShouldWakeUp() => actor.isVisible && UnityEngine.Random.value < chanceToWakeUpWhileVisible;
    while (!ShouldWakeUp()) {
      if (wakeUpNextTurn) {
        yield break;
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
  }

  /// doubles damage taken while sleeping!
  public int Modify(int input) {
    return input * 2;
  }
}

class DeepSleepTask : SleepTask {
  public DeepSleepTask(Actor actor, int turns) : base(actor) {
    this.turns = turns;
  }

  int turns;

  public override IEnumerator<BaseAction> Enumerator() {
    for(; turns >= 0; turns--) {
      if (wakeUpNextTurn) {
        yield break;
      }
      yield return new WaitBaseAction(actor);
    }
  }
}
