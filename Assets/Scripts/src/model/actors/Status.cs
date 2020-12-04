using System.Collections.Generic;
using System.Linq;

public abstract class Status {
  public virtual string displayName => Util.WithSpaces(GetType().Name.Replace("Status", ""));
}

public class StatusList {
  public List<Status> list;

  public StatusList(List<Status> statuses) {
    this.list = statuses;
  }

  public StatusList() : this(new List<Status>()) { }

  internal void Add(Status status) {
    this.list.Add(status);
  }

  internal void RemoveOfType<T>() where T : Status {
    var toRemove = this.list.Where((status) => status is T).ToList();
    foreach (var status in toRemove) {
      RemoveStatus(status);
    }
  }

  internal void RemoveStatus(Status status) {
    this.list.Remove(status);
  }

  internal IEnumerable<IActionCostModifier> ActionCostModifiers() {
    return ModifiersFor<IActionCostModifier, ActionCosts>();
  }

  internal IEnumerable<T> ModifiersFor<T, I>() where T : IModifier<I> {
    return this.list.Where((s) => s is T).Cast<T>();
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

public class CannotPerformActionException : System.Exception {
  string why;

  public CannotPerformActionException(string why) {
    this.why = why;
  }
}

public class SoftGrassStatus : Status, IActionCostModifier {
  public ActionCosts Modify(ActionCosts costs) {
    return new ActionCosts(costs) {
      // 20% faster
      [ActionType.MOVE] = costs[ActionType.MOVE] / (1 + 0.2f),
    };
  }
}
