public class AttackAction : ActorAction {
  public AttackAction(Actor actor, Actor _target) : base(actor) {
    target = _target;
  }

  public Actor target { get; }

  public override void Perform() {
    if (actor.IsNextTo(target)) {
      actor.Attack(target);
    }
    base.Perform();
  }
}
