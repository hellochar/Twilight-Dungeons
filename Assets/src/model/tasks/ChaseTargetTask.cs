using UnityEngine;
/// Open-ended.
[System.Serializable]
public class ChaseTargetTask : MoveNextToTargetTask {
  public override TaskStage WhenToCheckIsDone => TaskStage.Before;
  protected Body targetBody;
  private int extraMovesCutoff;
  public override Vector2Int target => targetBody.pos;

  public ChaseTargetTask(Actor actor, Body targetBody, int extraMovesCutoff = 0) : base(actor, targetBody.pos) {
    this.targetBody = targetBody;
    this.extraMovesCutoff = extraMovesCutoff;
  }

  public Body GetTargetBody() {
    return targetBody;
  }

  public override void PreStep() {
    if (targetBody == null || targetBody is Player p && p.isCamouflaged) {
      this.path.Clear();
    } else {
      this.path = FindBestAdjacentPath(actor, target);
      if (extraMovesCutoff > 0 && this.path.Count >= extraMovesCutoff) {
        this.path.RemoveRange(this.path.Count - extraMovesCutoff, extraMovesCutoff);
      }
    }
  }
}
