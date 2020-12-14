
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class EntityStore<T> : IEnumerable<T> where T : Entity {
  public Floor floor { get; }

  public T this[int x, int y] {
    get => Get(x, y);
  }

  public T this[Vector2Int vector] {
    get => Get(vector.x, vector.y);
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
public class StaticEntityGrid<T> : EntityStore<T> where T : Entity {
  private T[,] grid;
  public int width => floor.width;
  public int height => floor.height;

  public StaticEntityGrid(Floor floor) : base(floor) {
    this.grid = new T[floor.width, floor.height];
  }

  protected override T Get(int x, int y) => grid[x, y];

  public override void Put(T entity) {
    if (entity.floor != null) {
      throw new Exception($"Trying to re-Put non-moving Entity {entity}!");
    }

    var old = this[entity.pos];
    if (old != null) {
      // kill old grass; this indirectly also calls Remove()
      old.Kill();
    }

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
public class MovingEntityList<T> : EntityStore<T> where T : Entity {
  private List<T> list = new List<T>();

  public MovingEntityList(Floor floor) : base(floor) {}

  protected override T Get(int x, int y) => list.FirstOrDefault(a => a.pos.x == x && a.pos.y == y);

  /// <summary>Unlike the static grid, we do *not* Kill collided actors! Currently
  /// we allow multiple occupancy.</summary>
  public override void Put(T entity) {
    if (!floor.tiles[entity.pos.x, entity.pos.y].CanBeOccupied()) {
      Debug.LogWarning("Adding " + entity + " over a tile that cannot be occupied!");
    }
    // remove new Entity from old floor
    if (entity.floor != null) {
      entity.floor.Remove(entity);
    }
    list.Add(entity);
  }

  public override void Remove(T entity) {
    list.Remove(entity);
  }

  public override IEnumerator<T> GetEnumerator() {
    return list.GetEnumerator();
  }
}