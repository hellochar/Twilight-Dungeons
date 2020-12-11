using System;
using System.Collections.Generic;

/// Close-ended
public class GenericTask : DoOnceTask {
  public GenericTask(Actor actor, Action<Actor> action) : base(actor) {
    Name = Util.WithSpaces(Action.Method.Name);
    Action = action;
  }

  public Action<Actor> Action { get; }

  public override IEnumerator<BaseAction> Enumerator() {
    yield return new GenericBaseAction(actor, Action);
  }
}