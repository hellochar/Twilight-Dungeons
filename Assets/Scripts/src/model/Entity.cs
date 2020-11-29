using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Entity {
  public readonly Guid guid = System.Guid.NewGuid();
  public abstract Vector2Int pos { get; set; }
  public float timeCreated { get; }
  public float age => GameModel.main.time - timeCreated;
  public Floor floor;

  public Entity() {
    this.timeCreated = GameModel.main.time;
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
}
