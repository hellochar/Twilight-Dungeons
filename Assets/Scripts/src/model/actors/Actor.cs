using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

public class Actor : SteppableEntity {
  public static IDictionary<Type, float> ActionCosts = new ReadOnlyDictionary<Type, float>(
    new Dictionary<Type, float> {
      {typeof(ActorAction), 1}
    }
  );

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
  public int hp { get; protected set; }
  public int hpMax { get; protected set; }

  public virtual IDictionary<Type, float> actionCosts => Actor.ActionCosts;
  protected float baseActionCost => GetActionCost(typeof(ActorAction));
  /// how many turns this Entity has been alive for
  public virtual ActorAction action {
    get => actionQueue.FirstOrDefault();
    set => SetActions(value);
  }
  private List<ActorAction> actionQueue = new List<ActorAction>();
  public event Action<ActorAction> OnSetAction;

  public int visibilityRange = 7;
  public Faction faction = Faction.Neutral;
  public event Action<int, Actor> OnDealDamage;
  public event Action<int, int, Actor> OnTakeDamage;
  public event Action<int, int> OnHeal;
  /// gets called on a successful hit on a target
  public event Action<int, Actor> OnAttack;
  /// gets called on any ground targeted attack
  public event Action<Vector2Int, Actor> OnAttackGround;

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
    Actor target = floor.tiles[targetPosition.x, targetPosition.y].actor;
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
    base.Kill();
    /// TODO remove references to this Actor if needed
  }

  public void SetActions(params ActorAction[] actions) {
    actionQueue.Clear();
    actionQueue.AddRange(actions);
    OnSetAction?.Invoke(actionQueue[0]);
    RemoveDoneActions();
  }

  public void InsertActions(params ActorAction[] actions) {
    actionQueue.InsertRange(0, actions);
    OnSetAction?.Invoke(actionQueue[0]);
    RemoveDoneActions();
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

  protected override float Step() {
    RemoveDoneActions();
    ActorAction currentAction = this.action;
    float timeCost;
    if (currentAction == null) {
      timeCost = baseActionCost;
    } else {
      timeCost = GetActionCost(currentAction);
      currentAction.Perform();
      RemoveDoneActions();
    }
    return timeCost;
  }

  protected virtual void RemoveDoneActions() {
    while (actionQueue.Any() && actionQueue[0].IsDone()) {
      ActorAction finishedAction = actionQueue[0];
      actionQueue.RemoveAt(0);
      OnSetAction?.Invoke(actionQueue.FirstOrDefault());
      finishedAction.Finish();
    }
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