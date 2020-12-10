/// Open-ended.
public class ChaseTargetTask : MoveNextToTargetTask {
  protected Actor targetActor;

  public ChaseTargetTask(Actor actor, Actor targetActor) : base(actor, targetActor.pos) {
    this.targetActor = targetActor;
  }

  public override void OnGetNextPosition() {
    if (targetActor == null) {
      this.path.Clear();
    } else {
      this.path = FindBestAdjacentPath(actor.pos, targetActor.pos);
    }
  }

  public override bool IsDone() => false;
}
