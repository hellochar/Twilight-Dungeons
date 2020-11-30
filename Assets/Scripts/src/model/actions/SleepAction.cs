
/// Don't do anything until the Player's in view
class SleepAction : ActorAction {
  public bool wakeUpNextTurn { get; private set; }
  public SleepAction(Actor actor) : base(actor) {
  }

  public override void Perform() {
    if (wakeUpNextTurn) {
      base.Perform();
      return;
    }
    bool canSeePlayer = actor.tile.visibility == TileVisiblity.Visible;
    if (!canSeePlayer) {
      return;
    } else {
      // on wake-up, also wake up adjacent sleeping Actors
      foreach (var actor in actor.floor.AdjacentActors(actor.pos)) {
        if (actor.action is SleepAction s) {
          // hack to wake them up
          s.wakeUpNextTurn = true;
        }
      }
      base.Perform();
    }
  }
}
