using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IBodyMoveHandler {
  void HandleMove(Vector2Int newPos, Vector2Int oldPos);
}
public interface IBodyTakeAttackDamageHandler {
  void HandleTakeAttackDamage(int damage, int hp, Actor source);
}
public interface ITakeAnyDamageHandler {
  void HandleTakeAnyDamage(int damage);
}
public interface IHealHandler {
  void HandleHeal(int amount);
}
public interface IFloorChangeHandler {
  void HandleFloorChanged(Floor newFloor, Floor oldFloor);
}

[Serializable]
public class Body : Entity {
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
          floor.BodyMoved();
          OnMove(value, oldTile.pos);
          Tile newTile = floor.tiles[_pos];
          newTile.BodyEntered(this);
        } else {
          OnMoveFailed(value);
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
    floor.BodyMoved();
    OnMove(_pos, oldTile.pos);
    other.OnMove(oldTile.pos, _pos);
    newTile.BodyEntered(this);
    oldTile.BodyEntered(other);
  }

  public void ChangeFloors(Floor newFloor, Vector2Int newPos) {
    var oldFloor = floor;
    var oldTile = oldFloor?.tiles[_pos];

    oldTile?.BodyLeft(this);
    oldFloor?.Remove(this);

    _pos = newPos;

    // Recompute flag is already set by floor.Remove()
    // floor.BodyMoved();

    // this is NOT a move, so don't trigger OnMove
    // OnMove(newPos, oldTile.pos);

    var newTile = newFloor.tiles[newPos];
    newFloor.Put(this);

    OnFloorChanged(floor, oldFloor);

    newTile.BodyEntered(this);
  }

  public int hp { get; protected set; }
  public int baseMaxHp { get; protected set; }
  public virtual int maxHp => baseMaxHp;
  public override IEnumerable<object> MyModifiers => base.MyModifiers.Append(this.grass?.BodyModifier);
  [field:NonSerialized]
  public event Action OnMaxHPAdded;

  public Body(Vector2Int pos) : base() {
    this.pos = pos;
  }

  protected override void HandleLeaveFloor() {
    tile.BodyLeft(this);
  }

  protected override void HandleEnterFloor() {
    tile.BodyEntered(this);
  }

  protected virtual void OnFloorChanged(Floor newFloor, Floor oldFloor) {
    foreach (var handler in this.Of<IFloorChangeHandler>()) {
      handler.HandleFloorChanged(newFloor, oldFloor);
    }
  }

  /// returns how much it actually healed
  internal int Heal(int amount) {
    if (amount <= 0) {
      Debug.Log("tried healing <= 0");
      return 0;
    }
    amount = Mathf.Clamp(amount, 0, maxHp - hp);
    hp += amount;
    OnHeal(amount);
    return amount;
  }

  public void AddMaxHP(int amount) {
    baseMaxHp += amount;
    OnMaxHPAdded?.Invoke();
  }

  public void Attacked(int damage, Actor source) {
    TakeAttackDamage(damage, source);
  }

  /// Attack damage doesn't always come from an *attack* specifically. For instance,
  /// Snail shells count as attack damage, although they are not an attack action.
  public void TakeAttackDamage(int damage, Actor source) {
    if (IsDead) {
      return;
    }
    damage = Modifiers.Process(this.AttackDamageTakenModifiers(), damage);
    damage = Math.Max(damage, 0);
    source.OnDealAttackDamage(damage, this);
    OnTakeAttackDamage(damage, hp, source);
    TakeDamage(damage, source);
  }

  /// Take damage from any source.
  public void TakeDamage(int damage, Entity source) {
    if (IsDead) {
      return;
    }
    damage = Modifiers.Process(this.AnyDamageTakenModifiers(), damage);
    TakeUnavoidableDamage(damage, source);
  }

  public void TakeUnavoidableDamage(int damage, Entity source) {
    if (IsDead) {
      return;
    }
    damage = Math.Max(damage, 0);
    OnTakeAnyDamage(damage);
    hp -= damage;
    if (hp <= 0) {
      Kill(source);
    }
  }

  public override void Kill(Entity source) {
    /// TODO remove references to this Actor if needed
    if (!IsDead) {
      hp = Math.Max(hp, 0);
      base.Kill(source);
    }
  }

  protected virtual void OnMoveFailed(Vector2Int wantedPos) {}

  private void OnMove(Vector2Int newPos, Vector2Int oldPos) {
    foreach (var handler in this.Of<IBodyMoveHandler>()) {
      handler.HandleMove(newPos, oldPos);
    }
  }

  private void OnTakeAttackDamage(int dmg, int hp, Actor source) {
    foreach (var handler in this.Of<IBodyTakeAttackDamageHandler>()) {
      handler.HandleTakeAttackDamage(dmg, hp, source);
    }
  }

  private void OnTakeAnyDamage(int dmg) {
    foreach (var handler in this.Of<ITakeAnyDamageHandler>()) {
      handler.HandleTakeAnyDamage(dmg);
    }
  }

  private void OnHeal(int amount) {
    foreach (var handler in this.Of<IHealHandler>()) {
      handler.HandleHeal(amount);
    }
  }
}