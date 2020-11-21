public class ChaseTargetAction : MoveNextToTargetAction {
  private readonly Actor targetActor;

  public ChaseTargetAction(Actor actor, Actor targetActor) : base(actor, targetActor.pos) {
    this.targetActor = targetActor;
  }

  public override float Perform() {
    /// recompute the path
    this.path = FindBestAdjacentPath(actor.pos, targetActor.pos);
    return base.Perform();
  }
}
