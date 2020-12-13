using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Entity {
  public bool IsDead { get; private set; }
  public readonly Guid guid = System.Guid.NewGuid();
  public abstract Vector2Int pos { get; set; }
  public float timeCreated { get; }
  public float age => GameModel.main.time - timeCreated;
  public Floor floor { get; private set; }
  public Tile tile => floor.tiles[pos.x, pos.y];
  public Grass grass => floor.GrassAt(pos);
  public Actor actor => floor.ActorAt(pos);
  public bool isVisible => IsDead ? false : tile.visibility == TileVisiblity.Visible;
  /// called after the new floor is set
  public event Action OnEnterFloor;
  /// called before the old floor is left
  public event Action OnLeaveFloor;
  public event Action OnDeath;

  public Entity() {
    this.timeCreated = GameModel.main.time;
  }

  /// Should only be called from Floor to internally update this Entity's floor pointer.
  public void SetFloor(Floor floor) {
    if (this.floor != null) {
      OnLeaveFloor?.Invoke();
    }
    this.floor = floor;
    if (this.floor != null) {
      OnEnterFloor?.Invoke();
    }
  }

  public float DistanceTo(Entity other) {
    return Vector2Int.Distance(pos, other.pos);
  }

  public bool IsNextTo(Entity other) {
    return IsNextTo(other.pos);
  }

  public bool IsNextTo(Vector2Int other) {
    return Math.Abs(pos.x - other.x) <= 1 && Math.Abs(pos.y - other.y) <= 1;
  }

  public override string ToString() {
    return $"{base.ToString()} ({guid.ToString().Substring(0, 6)})";
  }

  public virtual void Kill() {
    /// TODO remove references to this Actor if needed
    IsDead = true;
    OnDeath?.Invoke();
    floor.Remove(this);
  }
}