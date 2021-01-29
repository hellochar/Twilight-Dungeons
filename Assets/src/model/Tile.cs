using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public abstract class Tile : Entity {
  public TileVisiblity visibility = TileVisiblity.Unexplored;
  private Vector2Int _pos;

  public override Vector2Int pos {
    get => _pos;
    /// do not allow moving tiles
    set { }
  }

  public override IEnumerable<object> MyModifiers => base.MyModifiers.Append(grass).Append(item);

  public Tile(Vector2Int pos) : base() {
    this._pos = pos;
  }

  protected override void HandleEnterFloor() {
    if (GameModel.main?.player != null) {
      if (floor == GameModel.main.player.floor && floor.TestVisibility(GameModel.main.player.pos, pos)) {
        visibility = TileVisiblity.Visible;
      }
    }
  }

  /// 0.0 means unwalkable.
  /// weight 1 is "normal" weight.
  public float GetPathfindingWeight() {
    var weight = 0f;
    if (body != null) {
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

  internal void BodyLeft(Body body) {
    if (body is Actor actor) {
      GameModel.main.EnqueueEvent(() => {
        foreach (var handler in this.Of<IActorLeaveHandler>()) {
          handler.HandleActorLeave(actor);
        }
      });
    }
  }

  internal void BodyEntered(Body body) {
    if (body is Actor actor) {
      GameModel.main.EnqueueEvent(() => {
        foreach (var handler in this.Of<IActorEnterHandler>()) {
          handler.HandleActorEnter(actor);
        }
      });
    }
  }

  public virtual float BasePathfindingWeight() {
    return 1;
  }

  public bool ObstructsVision() {
    return BasePathfindingWeight() == 0 || body is IBlocksVision;
  }

  internal virtual bool CanBeOccupied() {
    return GetPathfindingWeight() != 0;
  }
}

public interface IActorEnterHandler {
  void HandleActorEnter(Actor who);
}

public interface IActorLeaveHandler {
  void HandleActorLeave(Actor who);
}

[Serializable]
public enum TileVisiblity {
  Unexplored, Visible, Explored
}

[Serializable]
public class Ground : Tile {
  public Ground(Vector2Int pos) : base(pos) { }
}

public class FancyGround : Ground {
  public FancyGround(Vector2Int pos) : base(pos) {
  }
}

[ObjectInfo(description: "Blocks vision and movement.", flavorText: "This hard rock has weathered centuries of erosion.")]
[Serializable]
public class Wall : Tile {
  public Wall(Vector2Int pos) : base(pos) { }
  public override float BasePathfindingWeight() {
    return 0;
  }
}

[Serializable]
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

[Serializable]
public class Downstairs : Tile, IActorEnterHandler {
  /// <summary>Where the player will be after taking the Upstairs connected to this tile.</summary>
  public Vector2Int landing => pos + new Vector2Int(-1, 0);
  public Downstairs(Vector2Int pos) : base(pos) {}

  public void HandleActorEnter(Actor actor) {
    var player = GameModel.main.player;
    if (actor == player) {
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

[ObjectInfo(description: "Plant seeds in Soil.", flavorText: "Good soil is hard to come by in the caves...")]
[Serializable]
public class Soil : Tile {
  public Soil(Vector2Int pos) : base(pos) { }
}

[ObjectInfo(description: "Tap to collect. Planting a seed costs 1 water.", flavorText: "Water water everywhere...")]
[Serializable]
public class Water : Tile {
  public Water(Vector2Int pos) : base(pos) {
  }

  public void Collect(Player player) {
    player.water++;
    floor.Put(new Ground(pos));
  }
}