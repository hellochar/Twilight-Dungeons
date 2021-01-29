using System;
using System.Collections.Generic;
using UnityEngine;

public delegate void OnDealAttackDamage(int dmg, Body target);

[Serializable]
public class Body : Entity, IModifierProvider {
  private static IEnumerable<object> SelfEnumerator<T>(T item) {
    yield return item;
  }
  public virtual IEnumerable<object> MyModifiers => SelfEnumerator(this);

  private Vector2Int _pos;
  public override Vector2Int pos {
    get => _pos;
    set {
      if (floor == null) {
        _pos = value;
      } else {
        if (floor.tiles[value.x, value.y].CanBeOccupied()) {
          var oldTile = floor.tiles[_pos];
          oldTile.BodyLeft(this);
          _pos = value;
          OnMove?.Invoke(value, oldTile.pos);
          Tile newTile = floor.tiles[_pos];
          newTile.BodyEntered(this);
        } else {
          OnMoveFailed?.Invoke(value, _pos);
        }
      }
    }
  }

  /// assumes other is on the same floor
  public void SwapPositions(Actor other) {
    var oldTile = floor.tiles[_pos];
    Tile newTile = floor.tiles[other._pos];
    oldTile.BodyLeft(this);
    newTile.BodyLeft(other);
    _pos = newTile.pos;
    other._pos = oldTile.pos;
    OnMove?.Invoke(_pos, oldTile.pos);
    other.OnMove?.Invoke(oldTile.pos, _pos);
    newTile.BodyEntered(this);
    oldTile.BodyEntered(other);
  }

  public int hp { get; protected set; }
  public int baseMaxHp { get; protected set; }
  public virtual int maxHp => baseMaxHp;

  [field:NonSerialized]
  /// <summary>new position, old position</summary>
  public event Action<Vector2Int, Vector2Int> OnMove;

  [field:NonSerialized]
  /// <summary>failed position, old position</summary>
  public event Action<Vector2Int, Vector2Int> OnMoveFailed;

  [field:NonSerialized]
  public event Action<int, int, Actor> OnTakeAttackDamage;

  [field:NonSerialized]
  public event Action<int> OnTakeAnyDamage;
  /// <summary>amount, new hp</summary>

  [field:NonSerialized]
  public event Action<int, int> OnHeal;

  [field:NonSerialized]
  /// <summary>Invoked when another Actor attacks this one - (damage, target).</summary>
  public event Action<int, Actor> OnAttacked;
  
  public Body(Vector2Int pos) : base() {
    this.pos = pos;
  }

  protected override void HandleLeaveFloor() {
    tile.BodyLeft(this);
  }

  protected override void HandleEnterFloor() {
    tile.BodyEntered(this);
  }

  /// returns how much it actually healed
  internal int Heal(int amount) {
    if (amount <= 0) {
      Debug.Log("tried healing <= 0");
      return 0;
    }
    amount = Mathf.Clamp(amount, 0, maxHp - hp);
    hp += amount;
    OnHeal?.Invoke(amount, hp);
    return amount;
  }

  public void Attacked(int damage, Actor source) {
    OnAttacked?.Invoke(damage, source);
    TakeAttackDamage(damage, source);
  }

  /// Attack damage doesn't always come from an *attack* specifically. For instance,
  /// Snail shells count as attack damage, although they are not an attack action.
  public void TakeAttackDamage(int damage, Actor source) {
    damage = Modifiers.Process(this.AttackDamageTakenModifiers(), damage);
    damage = Math.Max(damage, 0);
    source.OnDealAttackDamage?.Invoke(damage, this);
    OnTakeAttackDamage?.Invoke(damage, hp, source);
    TakeDamage(damage);
  }

  /// Take damage from any source.
  public void TakeDamage(int damage) {
    damage = Modifiers.Process(this.AnyDamageTakenModifiers(), damage);
    damage = Math.Max(damage, 0);
    OnTakeAnyDamage?.Invoke(damage);
    hp -= damage;
    if (hp <= 0) {
      Kill();
    }
  }

  public override void Kill() {
    /// TODO remove references to this Actor if needed
    if (!IsDead) {
      hp = Math.Max(hp, 0);
      base.Kill();
    }
  }
}