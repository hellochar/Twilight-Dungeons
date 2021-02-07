using System;
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
    OnMove(_pos, oldTile.pos);
    other.OnMove(oldTile.pos, _pos);
    newTile.BodyEntered(this);
    oldTile.BodyEntered(other);
  }

  public int hp { get; protected set; }
  public int baseMaxHp { get; protected set; }
  public virtual int maxHp => baseMaxHp;

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
    OnHeal(amount);
    return amount;
  }

  public void Attacked(int damage, Actor source) {
    TakeAttackDamage(damage, source);
  }

  /// Attack damage doesn't always come from an *attack* specifically. For instance,
  /// Snail shells count as attack damage, although they are not an attack action.
  public void TakeAttackDamage(int damage, Actor source) {
    damage = Modifiers.Process(this.AttackDamageTakenModifiers(), damage);
    damage = Math.Max(damage, 0);
    source.OnDealAttackDamage(damage, this);
    OnTakeAttackDamage(damage, hp, source);
    TakeDamage(damage, source);
  }

  /// Take damage from any source.
  public void TakeDamage(int damage, Entity source) {
    damage = Modifiers.Process(this.AnyDamageTakenModifiers(), damage);
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