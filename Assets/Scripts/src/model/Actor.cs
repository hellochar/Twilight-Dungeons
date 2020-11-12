using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Actor : Entity {
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
  public int baseActionCost => 1;
  public int timeCreated { get; }
  /// how many turns this Entity has been alive for
  /// this has a bug with CatchUpStep - age will jump
  public int age => GameModel.main.time - timeCreated;
  public int timeNextAction;
  public virtual ActorAction action {
    get => actionQueue.FirstOrDefault();
    set {
      actionQueue.Clear();
      if (value != null) {
        actionQueue.Add(value);
      }
    }
  }
  protected List<ActorAction> actionQueue = new List<ActorAction>();

  public int visibilityRange = 7;
  public Floor floor;
  public Tile currentTile => floor.tiles[pos.x, pos.y];
  public bool visible => currentTile.visiblity == TileVisiblity.Visible;

  /// This number allows tweaking of Actor order when they would otherwise be scheduled
  /// at the same time. This offset gets added to the timeNextAction, so higher numbers
  /// will come after lower numbers. This does *NOT* actually modify "time" which the
  /// actor takes the action. Player has offset 0 (aka always goes first). This number should
  /// be < 1.
  internal virtual float queueOrderOffset { get => 0.5f; }
  public Faction faction = Faction.Neutral;
  public event Action<int, int, Actor> OnTakeDamage;
  public event Action<int, Actor> OnAttack;

  public Actor(Vector2Int pos) {
    hp = 8;
    hpMax = 8;
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
    hp = Mathf.Clamp(hp + amount, 0, hpMax);
  }

  internal void Attack(Actor target) {
    int damage = UnityEngine.Random.Range(1, 3);
    OnAttack?.Invoke(damage, target);
    target.TakeDamage(damage, this);
  }

  private void TakeDamage(int damage, Actor source) {
    if (damage < 0) {
      Debug.LogWarning("Cannot take negative damage!");
      return;
    }
    hp -= damage;
    OnTakeDamage?.Invoke(damage, hp, source);
    if (hp <= 0) {
      Kill();
    }
  }

  public void Kill() {
    /// TODO remove references to this Actor if needed
    hp = Math.Max(hp, 0);
    floor.RemoveActor(this);
  }

  public void SetActions(params ActorAction[] actions) {
    action = actions[0];
    for (int i = 1; i < actions.Length; i++) {
      actionQueue.Add(actions[i]);
    }
  }

  public virtual void Step() {
    RemoveDoneActions();
    ActorAction currentAction = actionQueue[0];
    if (currentAction == null) {
      this.timeNextAction += baseActionCost;
    } else {
      int timeCost = currentAction.Perform();
      this.timeNextAction += timeCost;
      RemoveDoneActions();
    }
  }

  private void RemoveDoneActions() {
    while (actionQueue.Count > 0 && actionQueue[0].IsDone()) {
      ActorAction finishedAction = actionQueue[0];
      actionQueue.RemoveAt(0);
      finishedAction.Finish();
    }
  }

  private void PopActionQueue() {
    if (actionQueue.Count > 1) {
      actionQueue.RemoveAt(0);
    } else {
      actionQueue.Clear();
    }
  }

  public bool IsNextTo(Entity other) {
    return IsNextTo(other.pos);
  }

  public bool IsNextTo(Vector2Int other) {
    return Math.Abs(pos.x - other.x) <= 1 && Math.Abs(pos.y - other.y) <= 1;
  }

  public void CatchUpStep(int newTime) {
    // by default actors don't do anything; they just act as if they were paused
    this.timeNextAction = Mathf.Max(this.timeNextAction, newTime);
  }

  public override string ToString() {
    return $"{base.ToString()} ({guid.ToString().Substring(0, 6)})";
  }
}

public enum Faction { Ally, Neutral, Enemy }