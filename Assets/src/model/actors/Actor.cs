using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

public class ActionCosts : Dictionary<ActionType, float> {
  public ActionCosts(IDictionary<ActionType, float> dictionary) : base(dictionary) {}
  public ActionCosts() : base() {}
  public ActionCosts Copy() => new ActionCosts(this);
}

public class Actor : Body, ISteppable {
  public float timeNextAction { get; set; }
  public virtual float turnPriority => 50;

  public static ActionCosts StaticActionCosts = new ActionCosts {
    {ActionType.ATTACK, 1},
    {ActionType.GENERIC, 1},
    {ActionType.MOVE, 1},
    {ActionType.WAIT, 1},
  };

  public override IEnumerable<object> MyModifiers => base.MyModifiers.Concat(statuses.list).Append(this.task);

  public StatusList statuses;
  public override int maxHp => Modifiers.Process(this.MaxHPModifiers(), baseMaxHp);

  /// don't call directly; this doesn't use modifiers
  protected virtual ActionCosts actionCosts => Actor.StaticActionCosts;
  public float baseActionCost => GetActionCost(ActionType.WAIT);
  public virtual ActorTask task {
    get => taskQueue.FirstOrDefault();
    set => SetTasks(value);
  }
  public IEnumerable<ActorTask> tasks => taskQueue.AsEnumerable();
  protected List<ActorTask> taskQueue = new List<ActorTask>();
  public event Action<ActorTask> OnSetTask;
  public event Action<BaseAction, BaseAction> OnActionPerformed;

  public int visibilityRange = 7;
  public Faction faction = Faction.Neutral;
  public OnDealAttackDamage OnDealAttackDamage;
  /// gets called on a successful hit on a target
  public event Action<int, Body> OnAttack;
  /// gets called on any ground targeted attack
  public event Action<Vector2Int> OnAttackGround;

  public Actor(Vector2Int pos) : base(pos) {
    statuses = new StatusList(this);
    hp = baseMaxHp = 8;
    // this.timeNextAction = this.timeCreated + baseActionCost;
    this.timeNextAction = this.timeCreated;
  }

  /// create an Attack with the specified damage. This does *not* do damage modifiers.
  internal void Attack(Body target, int damage) {
    if (target.IsDead) {
      throw new CannotPerformActionException("Cannot attack dead target.");
    }
    OnAttack?.Invoke(damage, target);
    target.Attacked(damage, this);
  }

  /// Attack the target, using this Actor's final attack damage.
  internal void Attack(Body target) {
    Attack(target, GetFinalAttackDamage());
  }

  /// get one instance of an attack damage from this Actor
  internal virtual (int, int) BaseAttackDamage() {
    Debug.LogWarning(this + " using base GetAttackDamage");
    return (1, 2);
  }

  internal virtual int GetFinalAttackDamage() {
    var (min, max) = BaseAttackDamage();
    var baseDamage = UnityEngine.Random.Range(min, max + 1);
    var finalDamage = Modifiers.Process(this.AttackDamageModifiers(), baseDamage);
    return finalDamage;
  }

  internal void AttackGround(Vector2Int targetPosition) {
    Body target = floor.bodies[targetPosition];
    Grass grass = floor.grasses[targetPosition];
    OnAttackGround?.Invoke(targetPosition);
    if (target != null) {
      Attack(target);
    } else if (grass != null) {
      // kill the grass when it gets attacked
      grass.Kill();
    }
  }

  public override void Kill() {
    if (!IsDead) {
      taskQueue.Clear();
      TaskChanged();
      foreach (var handler in Modifiers.Of<IActorKilledHandler>(this)) {
        handler.OnKilled(this);
      }
      base.Kill();
    }
  }

  public void ClearTasks() {
    SetTasks();
  }

  public void SetTasks(params ActorTask[] tasks) {
    taskQueue.Clear();
    taskQueue.AddRange(tasks);
    TaskChanged();
  }

  public void InsertTasks(params ActorTask[] tasks) {
    taskQueue.InsertRange(0, tasks);
    TaskChanged();
  }

  /// Call when this.task is changed
  protected void TaskChanged() {
    OnSetTask?.Invoke(this.task);
  }

  /// uses modifiers
  public float GetActionCost(ActionType t) {
    return Modifiers.Process(this.ActionCostModifiers(), actionCosts.Copy())[t];
  }

  public virtual float GetActionCost(BaseAction action) {
    return GetActionCost(action.Type);
  }

  public virtual float Step() {
    if (task == null) {
      throw new NoActionException();
    }
    /// clear out all done actions from the queue
    while (!task.MoveNext()) {
      // this mutates action
      GoToNextTask();
      if (task == null) {
        throw new NoActionException();
      }
    }
    // at this point, we know the following things:
    // action is not null
    // action.MoveNext() has been called and it returned true
    var action = task.Current;
    BaseAction finalAction = Perform(action);
    if (IsDead) {
      throw new ActorDiedException();
    }
    Modifiers.Process(this.StepModifiers(), null);

    // handle close-ended actions
    while (task != null && task.IsDoneOrForceOpen()) {
      GoToNextTask();
    }
    return GetActionCost(finalAction);
  }

  public BaseAction Perform(BaseAction action) {
    var finalAction = Modifiers.Process(this.BaseActionModifiers(), action);
    finalAction.Perform();
    OnActionPerformed?.Invoke(finalAction, action);
    return finalAction;
  }

  /// Precondition: this.action's enumerator is ended, but the action is still in the queue.
  /// This will remove this.action from the queue and call .Finish() on it, which will
  /// indirectly set this.action to the next one in the queue
  protected virtual void GoToNextTask() {
    task.Ended();
    taskQueue.RemoveAt(0);
    TaskChanged();
  }
}

public enum Faction { Ally, Neutral, Enemy }

public class Attack {
  public readonly Body target;
  public int damage;
  public readonly Actor source;

  public Attack(Actor source, Body target, int damage) {
    this.source = source;
    this.target = target;
    this.damage = damage;
  }

  public Attack(Actor source, Body target) : this(source, target, source.GetFinalAttackDamage()) {
  }
}

// /// First we create an attack "instance", and
// /// fill it up with info.
// /// Then we "execute" the attack
// public class AttackInstance {
//   public AttackInstance(Actor source, Actor target) {
//     this.source = source;
//     this.target = target;
//   }

//   public Actor source { get; }
//   // source's weapon
//   public Item weapon { get; set; }
//   public Actor target { get; }
//   // target's armor
//   public Item armor { get; set; }

//   void Execute() {
//     int damage = source.GetAttackDamage();
//     if (damage < 0) {
//       Debug.LogWarning("Cannot take negative damage!");
//       return;
//     }
//     target.TakeDamage(damage, source);
//   }
// }