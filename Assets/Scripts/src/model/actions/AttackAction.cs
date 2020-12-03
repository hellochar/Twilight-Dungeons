using System.Collections.Generic;

public class AttackAction : DoOnceActorAction {
  public AttackAction(Actor actor, Actor _target) : base(actor) {
    target = _target;
  }

  public Actor target { get; }

  public override IEnumerator<BaseAction> Enumerator() {
    yield return new AttackBaseAction(actor, target);
  }
}
