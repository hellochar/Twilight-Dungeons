/// Open-ended.
[System.Serializable]
public class ChaseTargetTask : MoveNextToTargetTask {
  public override TaskStage WhenToCheckIsDone => TaskStage.Before;
  protected Actor targetActor;

  public ChaseTargetTask(Actor actor, Actor targetActor) : base(actor, targetActor.pos) {
    this.targetActor = targetActor;
  }

  public Actor GetTargetActor() {
    return targetActor;
  }

  public override void PreStep() {
    if (targetActor == null) {
      this.path.Clear();
    } else {
      this.path = FindBestAdjacentPath(actor.pos, targetActor.pos);
      UnityEngine.Debug.Log(string.Join(", ", this.path));
    }
  }
}
