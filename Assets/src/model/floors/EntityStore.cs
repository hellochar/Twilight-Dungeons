
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
public abstract class EntityStore<T> : IEnumerable<T> where T : Entity {
  public Floor floor { get; }

  public T this[int x, int y] {
    get => Get(x, y);
  }

  public T this[Vector2Int vector] {
    get => floor.InBounds(vector) ? Get(vector.x, vector.y) : default(T);
  }

  public EntityStore(Floor floor) {
    this.floor = floor;
  }

  protected abstract T Get(int x, int y);

  // Only call this from Floor. Should Kill() (which will then Remove()) old entity.
  public abstract void Put(T entity);

  // Only call this from Floor.
  public abstract void Remove(T entity);

  public bool Has(Vector2Int pos) {
    return this[pos] != null;
  }

  public abstract IEnumerator<T> GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}

/// Storing a dense grid of Entity's that don't move.
[Serializable]
public class StaticEntityGrid<T> : EntityStore<T> where T : Entity {
  private T[,] grid;
  private readonly Action<T> PlacementBehavior;

  public int width => floor.width;
  public int height => floor.height;

  public StaticEntityGrid(Floor floor, Action<T> placementBehavior = null) : base(floor) {
    this.grid = new T[floor.width, floor.height];
    if (placementBehavior == null) {
      placementBehavior = KillOldOnCollision;
    }

    this.PlacementBehavior = placementBehavior;
  }

  protected override T Get(int x, int y) => grid[x, y];

  private void KillOldOnCollision(T entity) {
    var old = this[entity.pos];
    if (old != null) {
      old.Kill(entity);
    }
  }

  public override void Put(T entity) {
    if (entity.floor != null) {
      throw new Exception($"Trying to re-Put non-moving Entity {entity}!");
    }
    PlacementBehavior(entity);

    grid[entity.pos.x, entity.pos.y] = entity;
  }

  public override void Remove(T entity) {
    grid[entity.pos.x, entity.pos.y] = null;
  }

  public override IEnumerator<T> GetEnumerator() {
    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        if (grid[x, y] != null) {
          yield return grid[x, y];
        }
      }
    }
  }
}

/// Storing a sparse list of entities that can move, both in positions and in floors.
[Serializable]
public class MovingEntityList<T> : EntityStore<T> where T : Entity {
  private List<T> list = new List<T>();

  [NonSerialized]
  private bool needsRecompute;
  /// should regenerate on load
  [NonSerialized]
  private T[,] grid;
  private readonly Action<T> PlacementBehavior;

  public MovingEntityList(Floor floor, Action<T> placementBehavior = null) : base(floor) {
    this.PlacementBehavior = placementBehavior;
    needsRecompute = true;
  }

  [OnDeserialized]
  void HandleDeserialized() {
    needsRecompute = true;
  }

  public void ScheduleRecompute() {
    needsRecompute = true;
  }

  protected override T Get(int x, int y) {
    if (needsRecompute || grid == null) {
      recomputeGrid();
    }
    return grid[x, y];
    // list.FirstOrDefault(a => a.pos.x == x && a.pos.y == y);
  }

  void recomputeGrid() {
    if (grid == null) {
      grid = new T[floor.width, floor.height];
    } else {
      Array.Clear(grid, 0, grid.Length);
    }
    foreach (var e in list) {
      if (grid[e.pos.x, e.pos.y] != null) {
        Debug.LogError("Bodies overlapping: " + grid[e.pos.x, e.pos.y] + " and " + e);
      }
      grid[e.pos.x, e.pos.y] = e;
    }
    needsRecompute = false;
  }

  /// <summary>Unlike the static grid, we do *not* Kill collided actors! Currently
  /// we allow multiple occupancy.</summary>
  public override void Put(T entity) {
    var tile = floor.tiles[entity.pos.x, entity.pos.y];
    if (!tile.CanBeOccupied()) {
      Debug.LogWarning("Adding " + entity + " over tile " + tile + " that cannot be occupied!");
    }
    /// we've collided with another entity; do the placement behavior.
    /// note we do NOT do this if you're on a wall, since Graspers
    /// should be able to be on Walls
    if (this[entity.pos] != null && PlacementBehavior != null) {
      PlacementBehavior(entity);
    }
    // remove new Entity from old floor
    if (entity.floor != null) {
      entity.floor.Remove(entity);
    }
    list.Add(entity);
    needsRecompute = true;
  }

  public override void Remove(T entity) {
    list.Remove(entity);
    needsRecompute = true;
  }

  public override IEnumerator<T> GetEnumerator() {
    return list.GetEnumerator();
  }
}