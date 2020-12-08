using System;
using System.Collections.Generic;
using System.Linq;

public abstract class Status {
  public event Action OnRemoved;

  public virtual string displayName => Util.WithSpaces(GetType().Name.Replace("Status", ""));
  public abstract string Info();

  internal void Removed() {
    OnRemoved?.Invoke();
  }
}

public class StatusList {
  public List<Status> list;

  public event Action<Status> OnAdded;

  public StatusList(List<Status> statuses) {
    this.list = statuses;
  }

  public StatusList() : this(new List<Status>()) { }

  public void Add(Status status) {
    this.list.Add(status);
    OnAdded?.Invoke(status);
  }

  public void RemoveOfType<T>() where T : Status {
    var toRemove = this.list.Where((status) => status is T).ToList();
    foreach (var status in toRemove) {
      Remove(status);
    }
  }

  public void Remove(Status status) {
    this.list.Remove(status);
    status.Removed();
  }

  internal IEnumerable<IActionCostModifier> ActionCostModifiers() {
    return ModifiersFor<IActionCostModifier, ActionCosts>();
  }

  internal IEnumerable<IBaseActionModifier> BaseActionModifiers() {
    return ModifiersFor<IBaseActionModifier, BaseAction>();
  }

  internal IEnumerable<T> ModifiersFor<T, I>() where T : IModifier<I> {
    return list.Where((s) => s is T).Cast<T>();
  }

  internal T FindOfType<T>() where T : Status {
    return (T) (list.Find((s) => s is T));
  }
}

interface IModifier<T> {
  T Modify(T input);
}

static class Modifiers {
  public static T Process<T>(IEnumerable<IModifier<T>> modifiers, T initial) {
    return modifiers.Aggregate(initial, (current, modifier) => modifier.Modify(current));
  }
}

interface IActionCostModifier : IModifier<ActionCosts> {}
interface IBaseActionModifier : IModifier<BaseAction> {}

public class CannotPerformActionException : System.Exception {
  string why;

  public CannotPerformActionException(string why) {
    this.why = why;
  }
}

public class SoftGrassStatus : Status, IActionCostModifier {
  public ActionCosts Modify(ActionCosts costs) {
    return new ActionCosts(costs) {
      // 33% faster
      [ActionType.MOVE] = costs[ActionType.MOVE] / 1.33f,
    };
  }

  public override string Info() => "Player moves 33% faster in Soft Grass.";
}

public class BoundStatus : Status, IBaseActionModifier {
  public int turnsLeft = 2;
  public override string Info() => "You must break free of vines before you can move!";

  public BaseAction Modify(BaseAction input) {
    if (input is MoveBaseAction) {
      return new StruggleBaseAction(input.actor);
    }
    return input;
  }
}
