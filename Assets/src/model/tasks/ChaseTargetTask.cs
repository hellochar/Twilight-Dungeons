/// Open-ended.
[System.Serializable]
public class ChaseTargetTask : MoveNextToTargetTask {
  public override TaskStage WhenToCheckIsDone => TaskStage.Before;
  protected Actor targetActor;
  private int extraMovesCutoff;

  public ChaseTargetTask(Actor actor, Actor targetActor, int extraMovesCutoff = 0) : base(actor, targetActor.pos) {
    this.targetActor = targetActor;
    this.extraMovesCutoff = extraMovesCutoff;
  }

  public Actor GetTargetActor() {
    return targetActor;
  }

  public override void PreStep() {
    if (targetActor == null) {
      this.path.Clear();
    } else {
      this.path = FindBestAdjacentPath(actor.pos, targetActor.pos);
      if (extraMovesCutoff > 0 && this.path.Count >= extraMovesCutoff) {
        this.path.RemoveRange(this.path.Count - extraMovesCutoff, extraMovesCutoff);
      }
    }
  }
}
