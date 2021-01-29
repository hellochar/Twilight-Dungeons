using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EntityLayer {
  NONE = 0,
  TILE = 1,
  GRASS = 2,
  ITEM = 4,
  BODY = 8,
}

[Serializable]
public abstract class Entity {
  public abstract EntityLayer layer { get; }
  public bool IsDead { get; private set; }
  public readonly Guid guid = System.Guid.NewGuid();
  public abstract Vector2Int pos { get; set; }
  public float timeCreated { get; }
  public float age => GameModel.main.time - timeCreated;
  public Floor floor { get; private set; }
  public Tile tile => floor.tiles[pos];
  public Grass grass => floor.grasses[pos];
  public ItemOnGround item => floor.items[pos];
  public Body body => floor.bodies[pos];
  public Actor actor => body as Actor;
  public virtual string displayName => Util.WithSpaces(GetType().Name);
  public virtual string description => ObjectInfo.GetDescriptionFor(this);

  public bool isVisible => IsDead ? false : tile.visibility == TileVisiblity.Visible;
  /// called after the new floor is set
  [field:NonSerialized]
  public event Action OnEnterFloor;
  /// called before the old floor is left
  [field:NonSerialized]
  public event Action OnLeaveFloor;
  [field:NonSerialized]
  public event Action OnDeath;

  public Entity() {
    this.timeCreated = GameModel.main.time;
  }

  /// <summary>Only call this from Floor to internally update this Entity's floor pointer.</summary>
  public void SetFloor(Floor floor) {
    if (this.floor != null) {
      OnLeaveFloor?.Invoke();
    }
    this.floor = floor;
    if (this.floor != null) {
      OnEnterFloor?.Invoke();
    }
  }

  public float DistanceTo(Vector2Int other) {
    return Vector2Int.Distance(pos, other);
  }

  public float DistanceTo(Entity other) {
    return DistanceTo(other.pos);
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
    if (!IsDead) {
      IsDead = true;
      OnDeath?.Invoke();
      floor.Remove(this);
    } else {
      Debug.LogWarning("Calling Kill() on already dead Entity! Ignoring");
    }
  }

  public TimedEvent AddTimedEvent(float time, Action action) {
    GameModel model = GameModel.main;
    var evt = new TimedEvent(this, model.time + time, action);
    model.turnManager.AddTimedEvent(evt);
    return evt;
  }
}
