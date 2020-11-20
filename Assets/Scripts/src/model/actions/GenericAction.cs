using System;

public class GenericAction : ActorAction {
  public GenericAction(Actor actor, Action<Actor> action) : base(actor) {
    Action = action;
  }

  public Action<Actor> Action { get; }

  public override int Perform() {
    Action.Invoke(actor);
    return base.Perform();
  }

  public override string displayName => Util.WithSpaces(Action.Method.Name);
}