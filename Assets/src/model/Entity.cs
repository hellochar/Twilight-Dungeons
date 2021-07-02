using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

public interface IDeathHandler {
  void HandleDeath(Entity source);
}

[Serializable]
public abstract class Entity : IModifierProvider {
  public readonly Guid guid = System.Guid.NewGuid();
  public readonly HashSet<TimedEvent> timedEvents = new HashSet<TimedEvent>();
  public bool IsDead { get; private set; }
  public Floor floor { get; private set; }
  public float timeCreated { get; }
  public abstract Vector2Int pos { get; set; }

  public float age => GameModel.main.time - timeCreated;
  public Tile tile => floor.tiles[pos];
  /// TODO remove null from floor
  public Grass grass => floor?.grasses[pos];
  public ItemOnGround item => floor?.items[pos];
  public Trigger trigger => floor?.triggers[pos]; /// TODO remove null from floor
  public Body body => floor?.bodies[pos];
  public Actor actor => body as Actor;
  public virtual string displayName => Util.WithSpaces(GetType().Name);
  public virtual string description => ObjectInfo.GetDescriptionFor(this);
  /// <summary>Entity is visible if the tile they're standin on is visible.</summary>
  public virtual bool isVisible => !IsDead && tile.visibility == TileVisiblity.Visible;
  public virtual IEnumerable<object> MyModifiers => nonserializedModifiers.Append(this).Append(trigger);
  [NonSerialized] /// nonserialized by design
  public List<object> nonserializedModifiers = new List<object>();

  [OnDeserialized]
  public void OnDeserialized() {
    nonserializedModifiers = new List<object>();
  }

  public Entity() {
    this.timeCreated = GameModel.main.time;
  }

  /// <summary>Only call this from Floor to internally update this Entity's floor pointer.</summary>
  public void SetFloor(Floor floor) {
    if (this.floor != null) {
      HandleLeaveFloor();
    }
    this.floor = floor;
    if (this.floor != null) {
      HandleEnterFloor();
    }
  }

  protected virtual void HandleEnterFloor() {}
  protected virtual void HandleLeaveFloor() {}

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
    return $"{base.ToString()} ({pos.x}, {pos.y}) {(IsDead ? "Dead" : "")} {(floor == null ? "floor=null" : "")}".Trim();
  }

  public void KillSelf() {
    Kill(this);
  }

  public virtual void Kill(Entity source) {
    /// TODO remove references to this Actor if needed
    if (!IsDead) {
      IsDead = true;
      OnDeath(source);
      floor.Remove(this);
    } else {
      Debug.LogWarning("Calling Kill() on already dead Entity! Ignoring");
    }
  }

  /// Take great care - timed events are serialized by the method name as a string; anonymous delegates
  /// names may change without warning. Code renames can also cause save corruption.
  public TimedEvent AddTimedEvent(float time, Action action) {
    GameModel model = GameModel.main;
    var evt = new TimedEvent(this, model.time + time, action);
    timedEvents.Add(evt);
    model.timedEvents.Register(evt);
    return evt;
  }

  private void OnDeath(Entity source) {
    foreach (var handler in this.Of<IDeathHandler>()) {
      handler.HandleDeath(source);
    }
  }
}

[Serializable]
public class TimedEvent {
  public readonly float time;

  /// serialized by method name as a string. Don't use anonymous delegates. Don't rename method.
  public readonly Action action;
  public readonly Entity owner;
  public TimedEvent(Entity owner, float time, Action action) {
    this.owner = owner;
    this.time = time;
    this.action = action;
  }

  /// If the owner's floor is the current floor
  internal bool IsInvalid() => owner.floor != GameModel.main.currentFloor;

  public void UnregisterFromOwner() {
    owner.timedEvents.Remove(this);
  }
}
