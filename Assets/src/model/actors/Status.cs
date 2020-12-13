using System;
using System.Collections.Generic;
using System.Linq;

public abstract class Status {
  public event Action OnRemoved;

  public virtual string displayName => Util.WithSpaces(GetType().Name.Replace("Status", ""));
  public abstract string Info();

  public virtual void Step() {}

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

  internal bool Has<T>() where T : Status {
    return this.list.Where((Status) => Status is T).Any();
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
    return Modifiers.ActionCostModifiers(list);
  }

  internal IEnumerable<IBaseActionModifier> BaseActionModifiers() {
    return Modifiers.BaseActionModifiers(list);
  }

  internal IEnumerable<IDamageTakenModifier> DamageTakenModifiers() {
    return Modifiers.DamageTakenModifiers(list);
  }

  internal IEnumerable<IAttackDamageModifier> AttackDamageModifiers() {
    return Modifiers.AttackDamageModifiers(list);
  }

  internal T FindOfType<T>() where T : Status {
    return (T) (list.Find((s) => s is T));
  }
}

public class CannotPerformActionException : System.Exception {
  public readonly string why;

  public CannotPerformActionException(string why) {
    this.why = why;
  }
}

public class SoftGrassStatus : Status, IActionCostModifier {
  Player player;

  public SoftGrassStatus(Player player) {
    this.player = player;
  }

  public ActionCosts Modify(ActionCosts costs) {
    return new ActionCosts(costs) {
      // 33% faster
      [ActionType.MOVE] = costs[ActionType.MOVE] / 1.33f,
    };
  }

  public override void Step() {
    if (!(player.grass is SoftGrass)) {
      GameModel.main.EnqueueEvent(() => this.player.statuses.Remove(this));
    }
  }

  public override string Info() => "Player moves 33% faster in Soft Grass.";
}

public class BoundStatus : Status, IBaseActionModifier {
  public int turnsLeft = 3;
  public override string Info() => $"You must break free of vines before you can move or attack!\n{(int)(turnsLeft / 3.0f * 100)}% bound.";

  public BaseAction Modify(BaseAction input) {
    if (input.Type == ActionType.MOVE || input.Type == ActionType.ATTACK) {
      turnsLeft--;
      if (turnsLeft <= 0) {
        GameModel.main.EnqueueEvent(() => input.actor.statuses.Remove(this));
        return input;
      } else {
        return new StruggleBaseAction(input.actor);
      }
    }
    return input;
  }
}