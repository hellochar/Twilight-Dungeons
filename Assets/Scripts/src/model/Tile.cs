using System;
using System.Linq;
using UnityEngine;

public abstract class Tile : Entity {
  public Vector2Int pos { get; }
  public TileVisiblity visiblity = TileVisiblity.Unexplored;
  internal Floor floor;

  Actor occupant {
    get => floor.ActorAt(pos);
  }

  public Guid guid { get; }

  public Tile(Vector2Int pos) {
    guid = Guid.NewGuid();
    this.pos = pos;
  }

  /// 0.0 means unwalkable.
  /// weight 1 is "normal" weight.
  public float GetPathfindingWeight() {
    if (occupant != null) {
      return 0;
    }
    return BasePathfindingWeight();
  }

  protected virtual float BasePathfindingWeight() {
    return 1;
  }

  public virtual bool ObstructsVision() {
    return BasePathfindingWeight() == 0;
  }

  internal bool CanBeOccupied() {
    return GetPathfindingWeight() != 0;
  }

  public virtual void OnPlayerEnter() {}
}

public enum TileVisiblity {
  Unexplored, Visible, Explored
}

public class Ground : Tile {
  public Ground(Vector2Int pos) : base(pos) { }
}

public class Wall : Tile {
  public Wall(Vector2Int pos) : base(pos) { }
  protected override float BasePathfindingWeight() {
    return 0;
  }
}

public class Upstairs : Tile {
  public Upstairs(Vector2Int pos) : base(pos) {
  }

  public override void OnPlayerEnter() {
    Floor prevFloor = GameModel.main.floors[GameModel.main.activeFloorIndex - 1];
    if (prevFloor != null) {
      GameModel.main.PutPlayerAt(prevFloor, true);
    }
  }
}

public class Downstairs : Tile {
  public Downstairs(Vector2Int pos) : base(pos) { }

  public override void OnPlayerEnter() {
    Floor nextFloor = GameModel.main.floors[GameModel.main.activeFloorIndex + 1];
    if (nextFloor != null) {
      GameModel.main.PutPlayerAt(nextFloor, false);
    }
  }
}

public class Dirt : Tile {
  public Dirt(Vector2Int pos) : base(pos) { }
}

public class Soil : Tile {
  public Soil(Vector2Int pos) : base(pos) {}
}