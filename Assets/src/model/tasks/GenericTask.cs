using System;
using System.Collections.Generic;

/// Close-ended
public class GenericTask : DoOnceTask {
  private ActionType _type;
  ActionType type => _type;
  public GenericTask(Actor actor, Action<Actor> action, ActionType type = ActionType.GENERIC) : base(actor) {
    _type = type;
    Action = action;
    Name = Util.WithSpaces(Action.Method.Name);
  }

  public Action<Actor> Action { get; }

  public override IEnumerator<BaseAction> Enumerator() {
    yield return new GenericBaseAction(actor, Action);
  }
}