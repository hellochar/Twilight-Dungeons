
using System.Collections.Generic;
/// Don't do anything until the Player's in view
class SleepTask : ActorTask {
  public bool wakeUpNextTurn { get; private set; }
  public SleepTask(Actor actor) : base(actor) {
  }

  public override IEnumerator<BaseAction> Enumerator() {
    while(actor.tile.visibility != TileVisiblity.Visible) {
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
}
