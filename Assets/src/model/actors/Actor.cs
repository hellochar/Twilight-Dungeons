using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

// Called when the attached actor attacks a target, before damage is dealt.
// damage is after Actor modifiers, but before target modifiers.
public interface IAttackHandler {
  void OnAttack(int damage, Body target);
}

/// Called when Actor deals attack damage (including 0). This includes non-attack sources.
public interface IDealAttackDamageHandler {
  /// damage is after both Actor modifiers and target modifiers
  void HandleDealAttackDamage(int damage, Body target);
}

public interface IActionPerformedHandler {
  void HandleActionPerformed(BaseAction final, BaseAction initial);
}

public interface IStatusAddedHandler {
  void HandleStatusAdded(Status status);
}

public interface IStatusRemovedHandler {
  void HandleStatusRemoved(Status status);
}

[Serializable]
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

  [field:NonSerialized] /// Controller only
  public event Action<ActorTask> OnSetTask;
  public Faction faction = Faction.Neutral;
  /// gets called on any ground targeted attack
  [field:NonSerialized] /// Controller only
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
    OnAttack(damage, target);
    target.Attacked(damage, this);
  }

  /// Attack the target, using this Actor's final attack damage.
  internal virtual void Attack(Body target) {
    Attack(target, GetFinalAttackDamage());
  }

  /// get one instance of an attack damage from this Actor
  internal virtual (int, int) BaseAttackDamage() {
    return (0, 0);
  }

  internal virtual int GetFinalAttackDamage() {
    var (min, max) = BaseAttackDamage();
    var baseDamage = MyRandom.Range(min, max + 1);
    var finalDamage = Modifiers.Process(this.AttackDamageModifiers(), baseDamage);
    return finalDamage;
  }

  internal virtual void AttackGround(Vector2Int targetPosition) {
    Body target = floor.bodies[targetPosition];
    Grass grass = floor.grasses[targetPosition];
    OnAttackGround?.Invoke(targetPosition);
    if (target != null) {
      Attack(target);
    } else if (grass != null) {
      // kill the grass when it gets attacked
      grass.Kill(this);
    }
  }

  public override void Kill(Entity source) {
    if (!IsDead) {
      taskQueue.Clear();
      TaskChanged();
      foreach (var handler in Modifiers.Of<IActorKilledHandler>(this).ToList()) {
        handler.OnKilled(this);
      }
      base.Kill(source);
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
  protected virtual void TaskChanged() {
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
    task.PreStep();
    /// clear out all done actions from the queue
    while (task.IsDone()) {
      // this mutates action
      GoToNextTask();
      if (task == null) {
        throw new NoActionException();
      } else {
        task.PreStep();
      }
    }

    var isFree = task.isFreeTask;
    // at this point, we know task is not null and it is not done
    var action = task.GetNextAction();
    BaseAction finalAction = Perform(action);
    if (IsDead) {
      throw new ActorDiedException();
    }
    Modifiers.Process(this.StepModifiers(), null);
    task?.PostStep();

    // handle close-ended actions
    while (task != null && (task.WhenToCheckIsDone.HasFlag(TaskStage.After) && !task.forceOnlyCheckBefore) && task.IsDone()) {
      GoToNextTask();
    }

    if (isFree) {
      return 0;
    }
    return GetActionCost(finalAction);
  }

  public BaseAction Perform(BaseAction action) {
    var finalAction = Modifiers.Process(this.BaseActionModifiers(), action);
    finalAction.Perform();
    OnActionPerformed(finalAction, action);
    return finalAction;
  }

  /// Precondition: this.action's enumerator is ended, but the action is still in the queue.
  /// This will remove this.action from the queue and call .Finish() on it, which will
  /// indirectly set this.action to the next one in the queue
  public virtual void GoToNextTask() {
    task.Ended();
    taskQueue.RemoveAt(0);
    TaskChanged();
  }

  private void OnAttack(int damage, Body target) {
    foreach (var handler in this.Of<IAttackHandler>()) {
      handler.OnAttack(damage, target);
    }
  }
  
  private void OnActionPerformed(BaseAction final, BaseAction initial) {
    foreach (var handler in this.Of<IActionPerformedHandler>()) {
      handler.HandleActionPerformed(final, initial);
    }
  }

  /// trigger this Actor dealing attack damage.
  public void OnDealAttackDamage(int damage, Body target) {
    foreach (var handler in this.Of<IDealAttackDamageHandler>()) {
      handler.HandleDealAttackDamage(damage, target);
    }
  }

  public override string ToString() {
    return base.ToString() + $", HP {hp}/{maxHp}, Statuses: {string.Join(", ", statuses)}";
  }

  public bool CanTargetPlayer() {
    return isVisible;
  }
}

public enum Faction { Ally = 1, Neutral = 2, Enemy = 4 }

public class ActionCosts : Dictionary<ActionType, float> {
  public ActionCosts(IDictionary<ActionType, float> dictionary) : base(dictionary) {}
  public ActionCosts() : base() {}
  public ActionCosts Copy() => new ActionCosts(this);
}

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