using System;

internal class ChaseDynamicTargetAction : ChaseTargetAction {
  private Func<Actor> targetDecider;

  public ChaseDynamicTargetAction(Actor actor, Func<Actor> targetDecider, Actor initialTarget) : base(actor, initialTarget) {
    this.targetDecider = targetDecider;
  }

  public override void Perform() {
    this.targetActor = targetDecider();
    if (this.targetActor == null) {
      // end
      this.path.Clear();
    } else {
      base.Perform();
    }
  }
}