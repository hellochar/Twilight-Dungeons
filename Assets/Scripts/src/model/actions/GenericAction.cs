using System;

public class GenericAction : ActorAction {
  public GenericAction(Actor actor, Action<Actor> action) : base(actor) {
    Action = action;
  }

  public Action<Actor> Action { get; }

  public override void Perform() {
    Action.Invoke(actor);
    base.Perform();
  }

  public override string displayName => Util.WithSpaces(Action.Method.Name);
}