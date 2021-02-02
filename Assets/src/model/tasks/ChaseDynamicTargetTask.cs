using System;

[System.Serializable]
internal class ChaseDynamicTargetTask : ChaseTargetTask {
  private Func<Actor> targetDecider;

  public ChaseDynamicTargetTask(Actor actor, Func<Actor> targetDecider) : base(actor, targetDecider()) {
    this.targetDecider = targetDecider;
  }

  public override void PreStep() {
    this.targetActor = targetDecider();
    base.PreStep();
  }
}