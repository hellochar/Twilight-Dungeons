using System;
using System.Collections.Generic;

/// Close-ended
[Serializable]
public class GenericTask : DoOnceTask {
  public GenericTask(Actor actor, Action action) : base(actor) {
    Action = action;
    Name = Util.WithSpaces(Action.Method.Name);
  }

  public Action Action { get; }

  protected override BaseAction GetNextActionImpl() {
    return new GenericBaseAction(actor, Action);
  }
}

[Serializable]
public class GenericOneArgTask<T> : DoOnceTask {
  public Action<T> Action { get; }
  private T parameter { get; }

  /// Take care here - make sure the parameter is also serializable!
  public GenericOneArgTask(Actor actor, Action<T> action, T parameter) : base(actor) {
    this.Action = action;
    this.parameter = parameter;
    Name = Util.WithSpaces(Action.Method.Name);
  }

  protected override BaseAction GetNextActionImpl() {
    return new GenericBaseAction(actor, () => Action(parameter));
  }
}

/// doesn't need to be serialized because player tasks aren't saved.
public class GenericPlayerTask : GenericTask {
  public GenericPlayerTask(Player actor, Action action) : base(actor, action) {}
}

[Serializable]
public class SwapPositionsTask : DoOnceTask {
  private readonly Actor target;

  public SwapPositionsTask(Actor actor, Actor target) : base(actor) {
    this.target = target;
  }

  void Perform() {
    if (actor.IsNextTo(target)) {
      actor.SwapPositions(target);
    }
  }

  protected override BaseAction GetNextActionImpl() => new GenericBaseAction(actor, Perform, ActionType.MOVE);
}