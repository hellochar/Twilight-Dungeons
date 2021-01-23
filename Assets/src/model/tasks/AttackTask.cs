using System.Collections.Generic;

public class AttackTask : DoOnceTask {
  public AttackTask(Actor actor, Body target) : base(actor) {
    this.target = target;
  }

  public Body target { get; }

  public override IEnumerator<BaseAction> Enumerator() {
    yield return new AttackBaseAction(actor, target);
  }
}
