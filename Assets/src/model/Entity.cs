using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

public interface IDeathHandler {
  void HandleDeath(Entity source);
}

public interface IDeathInterceptor {
  bool InterceptDeath(Entity source);
}

[Serializable]
internal class CancelDeathException : Exception {
  public CancelDeathException() {
  }

  public CancelDeathException(string message) : base(message) {
  }

  public CancelDeathException(string message, Exception innerException) : base(message, innerException) {
  }

  protected CancelDeathException(SerializationInfo info, StreamingContext context) : base(info, context) {
  }
}

public interface IEntity {

}

public interface IHarvestable : IEntity {

}

[Serializable]
public abstract class Entity : IEntity, IModifierProvider {
  public readonly Guid guid = System.Guid.NewGuid();
  public readonly HashSet<TimedEvent> timedEvents = new HashSet<TimedEvent>();
  public bool IsDead { get; private set; }
  public Floor floor { get; private set; }
  public float timeCreated { get; }
  public abstract Vector2Int pos { get; set; }

  public static Vector2Int[] DefaultShape = new Vector2Int[1] { Vector2Int.zero };
  public virtual Vector2Int[] shape => DefaultShape;
  public IEnumerable<Vector2Int> area => shape.Select(vertex => vertex + pos);

  public float age => GameModel.main.time - timeCreated;
  public Tile tile => floor.tiles[pos];
#if experimental_chainfloors
  public Room room => floor?.rooms?.Find(r => r.isTerminal && r.Contains(pos));
#endif
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
    var oldFloor = this.floor;

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
    var xDist = Math.Abs(pos.x - other.x);
    var yDist = Math.Abs(pos.y - other.y);
#if experimental_nodiagonalmovement
    return xDist + yDist <= 1;
#else
    return xDist <= 1 && yDist <= 1;
#endif
  }

  public bool IsDiagonallyNextTo(Entity other) {
    return IsDiagonallyNextTo(other.pos);
  }

  public bool IsDiagonallyNextTo(Vector2Int other) {
    var xDist = Math.Abs(pos.x - other.x);
    var yDist = Math.Abs(pos.y - other.y);
    return xDist <= 1 && yDist <= 1;
  }

  public override string ToString() {
    return $"{base.ToString()}@({pos.x}, {pos.y}) {(IsDead ? "Dead" : "")} {(floor == null ? "floor=null" : "")}".Trim();
  }

  public void KillSelf() {
    Kill(this);
  }

  public virtual void Kill(Entity source) {
    /// TODO remove references to this Actor if needed
    if (!IsDead) {
      foreach (var interceptor in this.Of<IDeathInterceptor>()) {
        if (interceptor.InterceptDeath(source)) {
          // it's been intercepted! Stop dying; we expect the interceptor
          // to do whatever is needed to ensure you're ok
          return;
        }
      }
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
    foreach (var handler in source.Of<IKillEntityHandler>()) {
      handler.OnKill(this);
    }
  }

  public virtual void GetAvailablePlayerActions(List<MethodInfo> methods) {
    // no-op by default
  }
}

public static class EntityExtensions {
  public static IEnumerable<Tile> AreaAdjacentTiles(this Entity e) {
    return e.area.SelectMany(pos => e.floor.GetAdjacentTiles(pos)).Except(e.area.Select(p => e.floor.tiles[p]));
  }

  public static Item GetHomeItem(this Entity e) {
    var isHarvestableProperty = e.GetType().GetProperty("IsHarvestable");
    var isHarvestable = isHarvestableProperty != null ? ((bool)isHarvestableProperty.GetValue(e)) : true;
    if (!isHarvestable) {
      return null;
    }

    var homeItemProperty = e.GetType().GetProperty("HomeItem");
    if (homeItemProperty == null) {
      return null;
    }

    return homeItemProperty.GetValue(null) as Item;
  }
}