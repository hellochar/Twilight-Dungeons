using System;
using System.Collections.Generic;

/// Close-ended
[System.Serializable]
public class GenericTask : DoOnceTask {
  private ActionType _type;
  ActionType type => _type;
  /// TODO-SERIALIZE audit all Actions
  public GenericTask(Actor actor, Action<Actor> action, ActionType type = ActionType.GENERIC) : base(actor) {
    _type = type;
    Action = action;
    Name = Util.WithSpaces(Action.Method.Name);
  }

  public Action<Actor> Action { get; }

  protected override BaseAction GetNextActionImpl() {
    return new GenericBaseAction(actor, Action);
  }
}