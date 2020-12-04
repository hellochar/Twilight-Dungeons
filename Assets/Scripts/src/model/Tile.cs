using System;
using System.Linq;
using UnityEngine;

public abstract class Tile : Entity {
  public TileVisiblity visibility = TileVisiblity.Unexplored;
  private Vector2Int _pos;

  public override Vector2Int pos {
    get => _pos;
    /// do not allow moving tiles
    set { }
  }

  public event Action<Actor> OnActorEnter;
  public event Action<Actor> OnActorLeave;

  public Tile(Vector2Int pos) : base() {
    this._pos = pos;
  }

  /// 0.0 means unwalkable.
  /// weight 1 is "normal" weight.
  public float GetPathfindingWeight() {
    if (actor != null) {
      return 0;
    }
    return BasePathfindingWeight();
  }

  internal void ActorLeft(Actor actor) {
    GameModel.main.EnqueueEvent(() => OnActorLeave?.Invoke(actor));
  }

  internal void ActorEntered(Actor actor) {
    GameModel.main.EnqueueEvent(() => OnActorEnter?.Invoke(actor));
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
    OnActorEnter += HandleActorEnter;
  }

  public void HandleActorEnter(Actor actor) {
    if (actor == GameModel.main.player) {
      Floor prevFloor = GameModel.main.floors[GameModel.main.activeFloorIndex - 1];
      if (prevFloor != null) {
        GameModel.main.PutPlayerAt(prevFloor, true);
      }
    }
  }
}

public class Downstairs : Tile {
  public Downstairs(Vector2Int pos) : base(pos) {
    OnActorEnter += HandleActorEnter;
  }

  public void HandleActorEnter(Actor actor) {
    if (actor == GameModel.main.player) {
      Floor nextFloor = GameModel.main.floors[GameModel.main.activeFloorIndex + 1];
      if (nextFloor != null) {
        GameModel.main.PutPlayerAt(nextFloor, false);
      }
    }
  }
}

public class Soil : Tile {
  public Soil(Vector2Int pos) : base(pos) { }
}