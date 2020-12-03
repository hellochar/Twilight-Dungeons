using System;

internal class ChaseDynamicTargetTask : ChaseTargetTask {
  private Func<Actor> targetDecider;

  public ChaseDynamicTargetTask(Actor actor, Func<Actor> targetDecider, Actor initialTarget) : base(actor, initialTarget) {
    this.targetDecider = targetDecider;
  }

  public override void OnGetNextPosition() {
    this.targetActor = targetDecider();
    base.OnGetNextPosition();
  }
}