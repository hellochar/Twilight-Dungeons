using System;
using System.Linq;
using UnityEngine;

public abstract class Tile : Entity {
  public override EntityLayer layer => EntityLayer.TILE;
  public TileVisiblity visibility = TileVisiblity.Unexplored;
  private Vector2Int _pos;

  public override Vector2Int pos {
    get => _pos;
    /// do not allow moving tiles
    set { }
  }

  public event Action<Body> OnActorEnter;
  public event Action<Body> OnActorLeave;

  public Tile(Vector2Int pos) : base() {
    this._pos = pos;
  }

  /// 0.0 means unwalkable.
  /// weight 1 is "normal" weight.
  public float GetPathfindingWeight() {
    var weight = 0f;
    if (actor != null) {
      weight = 0;
    } else {
      weight = BasePathfindingWeight();
    }
    if (grass is IPathfindingCostModifier mod) {
      return mod.Modify(weight);
    } else {
      return weight;
    }
  }

  internal void ActorLeft(Body actor) {
    GameModel.main.EnqueueEvent(() => OnActorLeave?.Invoke(actor));
  }

  internal void ActorEntered(Body actor) {
    GameModel.main.EnqueueEvent(() => OnActorEnter?.Invoke(actor));
  }

  public virtual float BasePathfindingWeight() {
    return 1;
  }

  public bool ObstructsVision() {
    return BasePathfindingWeight() == 0 || actor is IBlocksVision;
  }

  internal virtual bool CanBeOccupied() {
    return GetPathfindingWeight() != 0;
  }
}

public enum TileVisiblity {
  Unexplored, Visible, Explored
}

public class Ground : Tile {
  public Ground(Vector2Int pos) : base(pos) { }
}

public class FancyGround : Ground {
  public FancyGround(Vector2Int pos) : base(pos) {
  }
}

public class Wall : Tile {
  public Wall(Vector2Int pos) : base(pos) { }
  public override float BasePathfindingWeight() {
    return 0;
  }
}

public class Upstairs : Tile {
  /// <summary>Where the player will be after taking the Downstairs connected to this tile.</summary>
  public Vector2Int landing => pos + new Vector2Int(1, 0);
  public Upstairs(Vector2Int pos) : base(pos) {}

  public void GoHome() {
    if (GameModel.main.player.IsInCombat()) {
      throw new CannotPerformActionException("There are enemies around!");
    }

    Floor prevFloor = GameModel.main.floors[0];
    GameModel.main.PutPlayerAt(prevFloor, true);
  }
}

public class Downstairs : Tile {
  /// <summary>Where the player will be after taking the Upstairs connected to this tile.</summary>
  public Vector2Int landing => pos + new Vector2Int(-1, 0);
  public Downstairs(Vector2Int pos) : base(pos) {
    OnActorEnter += HandleActorEnter;
  }

  public void HandleActorEnter(Body body) {
    var player = GameModel.main.player;
    if (body == player) {
      // if we're on floor 0, go straight to the deepest floor
      // if we're on the deepest floor, go 1 deeper
      Floor nextFloor;
      if (floor.depth == 0) {
        nextFloor = GameModel.main.floors[player.deepestDepthVisited];
      } else {
        nextFloor = GameModel.main.floors[floor.depth + 1];
      }
      GameModel.main.PutPlayerAt(nextFloor, false);
    }
  }
}

public class Soil : Tile {
  public Soil(Vector2Int pos) : base(pos) { }
}
