using System;

[System.Serializable]
internal class ChaseDynamicTargetTask : ChaseTargetTask {
  private Func<Body> targetDecider;

  public ChaseDynamicTargetTask(Actor actor, Func<Body> targetDecider) : base(actor, targetDecider()) {
    this.targetDecider = targetDecider;
  }

  public override void PreStep() {
    this.targetBody = targetDecider();
    base.PreStep();
  }
}