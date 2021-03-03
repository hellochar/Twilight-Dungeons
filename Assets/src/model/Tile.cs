using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public abstract class Tile : Entity {
  public TileVisiblity visibility = TileVisiblity.Unexplored;
  private Vector2Int _pos;
  /// <summary>Tiles are visible if they've ever been seen.</summary>
  public override bool isVisible => !IsDead && tile.visibility != TileVisiblity.Unexplored;

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
    /// set correct visibility when the tile is dynamically added
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
[ObjectInfo(description: "Creatures can walk here. Grass can grow here.", flavorText: "Earth - the first element.")]
public class Ground : Tile {
  public Ground(Vector2Int pos) : base(pos) { }
}

[Serializable]
public class Signpost : Ground {
  public string text;
  public Signpost(Vector2Int pos, string text = "") : base(pos) {
    this.text = text;
  }
}

[Serializable]
[ObjectInfo(description: "Grass cannot grow on Hard Ground.", flavorText: "Any workable earth has been blown or washed away.")]
public class HardGround : Tile {
  public HardGround(Vector2Int pos) : base(pos) { }

  protected override void HandleEnterFloor() {
    base.HandleEnterFloor();
    grass?.Kill(this);
  }
}

[Serializable]
[ObjectInfo(flavorText: "")]
public class FancyGround : Ground {
  public FancyGround(Vector2Int pos) : base(pos) {
  }
}

[ObjectInfo(description: "Blocks vision and movement.", flavorText: "Hard earth that has weathered centuries of erosion; it's not going anywhere.")]
[Serializable]
public class Wall : Tile {
  public Wall(Vector2Int pos) : base(pos) { }
  public override float BasePathfindingWeight() {
    return 0;
  }
}

[Serializable]
[ObjectInfo(description: "Go back home.")]
public class Upstairs : Tile {
  /// <summary>Where the player will be after taking the Downstairs connected to this tile.</summary>
  public Vector2Int landing => pos + new Vector2Int(1, 0);
  public Upstairs(Vector2Int pos) : base(pos) {}

  protected override void HandleEnterFloor() {
    base.HandleEnterFloor();
    grass?.Kill(this);
  }

  public void GoHome() {
    if (GameModel.main.player.IsInCombat()) {
      throw new CannotPerformActionException("There are enemies around!");
    }

    GameModel.main.PutPlayerAt(0);
  }
}

[Serializable]
[ObjectInfo(description: "Go deeper into the dungeon.")]
public class Downstairs : Tile, IActorEnterHandler {
  /// <summary>Where the player will be after taking the Upstairs connected to this tile.</summary>
  public Vector2Int landing => pos + new Vector2Int(-1, 0);
  public Downstairs(Vector2Int pos) : base(pos) {}

  protected override void HandleEnterFloor() {
    base.HandleEnterFloor();
    grass?.Kill(this);
  }

  public void HandleActorEnter(Actor actor) {
    if (actor == GameModel.main.player) {
      if (floor.EnemiesLeft() == 0) {
        floor.PlayerGoDownstairs();
      } else {
        Messages.Create("Clear level first.");
      }
    }
  }
}

[ObjectInfo(description: "Plant seeds here.", flavorText: "Fresh, moist, and perfect for growing. Hard to come by in the caves.")]
[Serializable]
public class Soil : Tile {
  public Soil(Vector2Int pos) : base(pos) { }
}

[ObjectInfo(description: "Walk into to collect.", flavorText: "Water water everywhere...")]
[Serializable]
public class Water : Tile {
  public Water(Vector2Int pos) : base(pos) {
  }

  public void Collect(Player player) {
    player.water += MyRandom.Range(105, 120);
    floor.Put(new Ground(pos));
  }
}