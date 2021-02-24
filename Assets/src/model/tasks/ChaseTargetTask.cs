/// Open-ended.
[System.Serializable]
public class ChaseTargetTask : MoveNextToTargetTask {
  public override TaskStage WhenToCheckIsDone => TaskStage.Before;
  protected Body targetBody;
  private int extraMovesCutoff;

  public ChaseTargetTask(Actor actor, Body targetBody, int extraMovesCutoff = 0) : base(actor, targetBody.pos) {
    this.targetBody = targetBody;
    this.extraMovesCutoff = extraMovesCutoff;
  }

  public Body GetTargetBody() {
    return targetBody;
  }

  public override void PreStep() {
    if (targetBody == null) {
      this.path.Clear();
    } else {
      this.path = FindBestAdjacentPath(actor.pos, targetBody.pos);
      if (extraMovesCutoff > 0 && this.path.Count >= extraMovesCutoff) {
        this.path.RemoveRange(this.path.Count - extraMovesCutoff, extraMovesCutoff);
      }
    }
  }
}
