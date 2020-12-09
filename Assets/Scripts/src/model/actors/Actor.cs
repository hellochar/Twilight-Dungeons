using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

public class ActionCosts : Dictionary<ActionType, float> {
  public ActionCosts(IDictionary<ActionType, float> dictionary) : base(dictionary) {}
  public ActionCosts() : base() {}
}

public class Actor : SteppableEntity {
  public static ActionCosts StaticActionCosts = new ActionCosts {
    {ActionType.ATTACK, 1},
    {ActionType.GENERIC, 1},
    {ActionType.MOVE, 1},
    {ActionType.WAIT, 1},
  };

  private Vector2Int _pos;
  public override Vector2Int pos {
    get => _pos;
    set {
      if (floor == null) {
        _pos = value;
      } else {
        if (floor.tiles[value.x, value.y].CanBeOccupied()) {
          var oldTile = floor.tiles[_pos];
          oldTile.ActorLeft(this);
          _pos = value;
          Tile newTile = floor.tiles[_pos];
          newTile.ActorEntered(this);
        }
      }
    }
  }

  public StatusList statuses = new StatusList();
  public int hp { get; protected set; }
  public int hpMax { get; protected set; }

  /// don't call directly; this doesn't use modifiers
  protected virtual ActionCosts actionCosts => Actor.StaticActionCosts;
  protected float baseActionCost => GetActionCost(ActionType.WAIT);
  /// how many turns this Entity has been alive for
  public virtual ActorTask task {
    get => taskQueue.FirstOrDefault();
    set => SetTasks(value);
  }
  protected List<ActorTask> taskQueue = new List<ActorTask>();
  public event Action<ActorTask> OnSetTask;
  public event Action<BaseAction, BaseAction> OnActionPerformed;

  public int visibilityRange = 7;
  public Faction faction = Faction.Neutral;
  public event Action<int, Actor> OnDealDamage;
  public event Action<int, int, Actor> OnTakeDamage;
  public event Action<int, int> OnHeal;
  /// gets called on a successful hit on a target
  public event Action<int, Actor> OnAttack;
  /// gets called on any ground targeted attack
  public event Action<Vector2Int> OnAttackGround;

  public Actor(Vector2Int pos) : base() {
    hp = hpMax = 8;
    this.timeNextAction = this.timeCreated + baseActionCost;
    this.pos = pos;
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
  }

  private void HandleLeaveFloor() {
    tile.ActorLeft(this);
  }

  private void HandleEnterFloor() {
    tile.ActorEntered(this);
  }

  /// returns how much it actually healed
  internal int Heal(int amount) {
    if (amount <= 0) {
      Debug.Log("tried healing <= 0");
      return 0;
    }
    amount = Mathf.Clamp(amount, 0, hpMax - hp);
    hp += amount;
    OnHeal?.Invoke(amount, hp);
    return amount;
  }

  /// create an Attack and execute it
  internal void Attack(Actor target) {
    if (target.IsDead) {
      throw new CannotPerformActionException("Cannot attack dead target.");
    }
    int damage = GetAttackDamage();
    OnAttack?.Invoke(damage, target);
    target.TakeDamage(damage, this);
  }

  /// get one instance of an attack damage from this Actor
  internal virtual int GetAttackDamage() {
    Debug.LogWarning(this + " using base GetAttackDamage");
    return UnityEngine.Random.Range(1, 3);
  }

  internal void AttackGround(Vector2Int targetPosition) {
    Actor target = floor.ActorAt(targetPosition);
    Grass grass = floor.GrassAt(targetPosition);
    OnAttackGround?.Invoke(targetPosition);
    if (target != null) {
      Attack(target);
    } else if (grass != null) {
      // kill the grass when it gets attacked
      grass.Kill();
    }
  }

  public void TakeDamage(int damage, Actor source) {
    if (damage < 0) {
      Debug.LogWarning("Cannot take negative damage!");
      return;
    }
    damage = ModifyDamage(damage);
    damage = Math.Max(damage, 0);
    hp -= damage;
    source.OnDealDamage?.Invoke(damage, this);
    OnTakeDamage?.Invoke(damage, hp, source);
    if (hp <= 0) {
      Kill();
    }
  }

  protected virtual int ModifyDamage(int damage) {
    return damage;
  }

  public override void Kill() {
    hp = Math.Max(hp, 0);
    taskQueue.Clear();
    TaskChanged();
    base.Kill();
    /// TODO remove references to this Actor if needed
  }

  public void ClearTasks() {
    SetTasks();
  }

  public void SetTasks(params ActorTask[] actions) {
    taskQueue.Clear();
    taskQueue.AddRange(actions);
    TaskChanged();
  }

  public void InsertTasks(params ActorTask[] actions) {
    taskQueue.InsertRange(0, actions);
    TaskChanged();
  }

  /// Call when this.task is changed
  protected void TaskChanged() {
    OnSetTask?.Invoke(this.task);
  }

  /// uses modifiers
  public float GetActionCost(ActionType t) {
    return Modifiers.Process(statuses.ActionCostModifiers(), actionCosts)[t];
  }

  public virtual float GetActionCost(BaseAction action) {
    return GetActionCost(action.Type);
  }

  protected override float Step() {
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
    var finalAction = Modifiers.Process(this.statuses.BaseActionModifiers(), action);
    finalAction.Perform();
    this.statuses.list.ForEach((status) => status.Step());
    OnActionPerformed?.Invoke(finalAction, action);

    // handle close-ended actions
    while (task != null && task.IsDone()) {
      GoToNextTask();
    }
    return GetActionCost(finalAction);
  }

  /// Precondition: this.action's enumerator is ended, but the action is still in the queue.
  /// This will remove this.action from the queue and call .Finish() on it, which will
  /// indirectly set this.action to the next one in the queue
  protected virtual void GoToNextTask() {
    taskQueue.RemoveAt(0);
    TaskChanged();
  }
}

public enum Faction { Ally, Neutral, Enemy }

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

public class NoActionException : Exception {}