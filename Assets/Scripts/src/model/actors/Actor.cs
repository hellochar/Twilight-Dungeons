using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

public class Actor : Entity {
  public static IDictionary<Type, float> ActionCosts = new ReadOnlyDictionary<Type, float>(
    new Dictionary<Type, float> {
      {typeof(ActorAction), 1}
    }
  );
  public bool IsDead { get; private set; }

  public Guid guid { get; }
  private Vector2Int _pos;
  public virtual Vector2Int pos {
    get => _pos;
    set {
      Floor floor = this.floor;
      if (floor == null || floor.tiles[value.x, value.y].CanBeOccupied()) {
        _pos = value;
      }
    }
  }
  public int hp { get; protected set; }
  public int hpMax { get; protected set; }

  internal float DistanceTo(Actor other) {
    return Vector2Int.Distance(pos, other.pos);
  }

  public virtual IDictionary<Type, float> actionCosts => Actor.ActionCosts;
  protected float baseActionCost => GetActionCost(typeof(ActorAction));
  public float timeCreated { get; }
  /// how many turns this Entity has been alive for
  public float age => GameModel.main.time - timeCreated;
  public float timeNextAction;
  public virtual ActorAction action {
    get => actionQueue.FirstOrDefault();
    set => SetActions(value);
  }
  private List<ActorAction> actionQueue = new List<ActorAction>();
  public event Action<ActorAction> OnSetAction;

  public int visibilityRange = 7;
  public Floor floor;
  public Tile currentTile => floor.tiles[pos.x, pos.y];
  public bool visible => currentTile.visibility == TileVisiblity.Visible;

  /// Determines Actor order when multiple have the same timeNextAction.
  /// Lower numbers go first.
  /// Player has offset 10 (usually goes first).
  /// Generally ranges in [0, 100].
  internal virtual float turnPriority => 50;

  public Faction faction = Faction.Neutral;
  public event Action<int, int, Actor> OnTakeDamage;
  public event Action<int, int> OnHeal;
  /// gets called on a successful hit on a target
  public event Action<int, Actor> OnAttack;
  /// gets called on any ground targeted attack
  public event Action<Vector2Int, Actor> OnAttackGround;
  public event Action<ActorAction, float> OnStepped;
  public event Action OnPreStep;
  public event Action OnDeath;

  public Actor(Vector2Int pos) {
    hp = hpMax = 8;
    guid = System.Guid.NewGuid();
    this.timeCreated = GameModel.main.time;
    this.timeNextAction = this.timeCreated + baseActionCost;
    this.pos = pos;
  }

  internal void Heal(int amount) {
    if (amount <= 0) {
      Debug.Log("tried healing <= 0");
      return;
    }
    amount = Mathf.Clamp(amount, 0, hpMax - hp);
    hp += amount;
    OnHeal?.Invoke(amount, hp);
  }

  internal void Attack(Actor target) {
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
    Actor target = floor.tiles[targetPosition.x, targetPosition.y].occupant;
    OnAttackGround?.Invoke(targetPosition, target);
    if (target != null) {
      Attack(target);
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
    OnTakeDamage?.Invoke(damage, hp, source);
    if (hp <= 0) {
      Kill();
    }
  }

  protected virtual int ModifyDamage(int damage) {
    return damage;
  }

  public void Kill() {
    /// TODO remove references to this Actor if needed
    IsDead = true;
    OnDeath?.Invoke();
    hp = Math.Max(hp, 0);
    floor.RemoveActor(this);
  }

  public void SetActions(params ActorAction[] actions) {
    if (actions.Where((a) => a == null).Count() > 0) {
      throw new Exception("Setting a null action!");
    }
    actionQueue.Clear();
    actionQueue.AddRange(actions);
    if (actions.Length > 0) {
      OnSetAction?.Invoke(actionQueue[0]);
    }
  }

  public float GetActionCost(Type t) {
    while (t != typeof(object)) {
      if (actionCosts.ContainsKey(t)) {
        return actionCosts[t];
      } else {
        // walk up the type hierarchy
        t = t.BaseType;
      }
    }
    return actionCosts[typeof(ActorAction)];
  }

  public float GetActionCost(ActorAction action) {
    return GetActionCost(action.GetType());
  }

  public virtual void Step() {
    RemoveDoneActions();
    OnPreStep?.Invoke();
    ActorAction currentAction = this.action;
    float timeCost;
    if (currentAction == null) {
      timeCost = baseActionCost;
    } else {
      timeCost = GetActionCost(currentAction);
      currentAction.Perform();
      RemoveDoneActions();
    }
    this.timeNextAction += timeCost;
    OnStepped?.Invoke(currentAction, timeCost);
  }

  protected virtual void RemoveDoneActions() {
    while (actionQueue.Any() && actionQueue[0].IsDone()) {
      ActorAction finishedAction = actionQueue[0];
      actionQueue.RemoveAt(0);
      OnSetAction?.Invoke(actionQueue.FirstOrDefault());
      finishedAction.Finish();
    }
  }

  public bool IsNextTo(Entity other) {
    return IsNextTo(other.pos);
  }

  public bool IsNextTo(Vector2Int other) {
    return Math.Abs(pos.x - other.x) <= 1 && Math.Abs(pos.y - other.y) <= 1;
  }

  public void CatchUpStep(float newTime) {
    // by default actors don't do anything; they just act as if they were paused
    this.timeNextAction = Mathf.Max(this.timeNextAction, newTime);
  }

  public override string ToString() {
    return $"{base.ToString()} ({guid.ToString().Substring(0, 6)})";
  }
}

public enum Faction { Ally, Neutral, Enemy }