using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public abstract class Tile : Entity {
  public TileVisiblity visibility = TileVisiblity.Unexplored;
  private Vector2Int _pos;
  public bool isExplored => !IsDead && tile.visibility != TileVisiblity.Unexplored;

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
    var player = GameModel.main?.player;
    if (player != null) {
      var isSameFloor = floor == player.floor;
      var isInVisibilityRange = DistanceTo(player) <= player.visibilityRange;
      var isVisible = floor.TestVisibility(player.pos, pos);
      if (isSameFloor && isInVisibilityRange && isVisible) {
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

  public virtual bool ObstructsVision() {
    return BasePathfindingWeight() == 0 || (body is IBlocksVision || grass is IBlocksVision);
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
[ObjectInfo("sign")]
public class Signpost : Ground {
  public bool hasRead;
  public string text;
  public Signpost(Vector2Int pos, string text = "") : base(pos) {
    this.text = text;
    hasRead = PlayerPrefs.HasKey("read-tip-"+text.GetHashCode());
  }

  public void ShowSignpost() {
    Popups.Create(
      title: "Tip",
      category: "",
      /// hack - add a linebreak before to put some space
      info: "\n" + text,
      flavor: "",
      sprite: null
    );
    hasRead = true;
    PlayerPrefs.SetInt("read-tip-"+text.GetHashCode(), 1);
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
[ObjectInfo(description: "Blocks movement.", flavorText: "You look down and cannot see the bottom. Be careful not to fall!")]
public class Chasm : Tile {
  public Chasm(Vector2Int pos) : base(pos) {
  }

  protected override void HandleEnterFloor() {
    base.HandleEnterFloor();
    // remove bodies and grasses on it. don't "kill" it though, since technically they're not dead
    if (grass != null) {
      floor.Remove(grass);
    }
    if (body != null && !(body is Actor)) {
      floor.Remove(body);
    }
    if (item != null) {
      floor.Remove(item);
      // this will trigger the item placement behavior and it will find an acceptable spot
      floor.Put(item);
    }
  }

  public override float BasePathfindingWeight() {
    return 0;
  }

  public override bool ObstructsVision() {
    return body is IBlocksVision || grass is IBlocksVision;
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

  public void TryGoHome() {
    if (floor.EnemiesLeft() == 0) {
      GameModel.main.PutPlayerAt(0);
    } else {
      var enemiesLeft = GameObject.Find("Enemies Left");
      var pulse = enemiesLeft.AddComponent<PulseAnimation>();
      pulse.pulseScale = 1.25f;
      throw new CannotPerformActionException();
    }
  }
}

[Serializable]
[ObjectInfo(description: "Go deeper into the dungeon.")]
public class Downstairs : Tile {
  /// <summary>Where the player will be after taking the Upstairs connected to this tile.</summary>
  public Vector2Int landing => pos + new Vector2Int(-1, 0);
  public Downstairs(Vector2Int pos) : base(pos) {}

  protected override void HandleEnterFloor() {
    base.HandleEnterFloor();
    grass?.Kill(this);
  }

  public void TryGoDownstairs() {
    if (floor.EnemiesLeft() == 0) {
      floor.PlayerGoDownstairs();
    } else {
      var enemiesLeft = GameObject.Find("Enemies Left");
      var pulse = enemiesLeft.AddComponent<PulseAnimation>();
      pulse.pulseScale = 1.25f;
      throw new CannotPerformActionException();
    }
  }
}

[ObjectInfo(description: "Plant seeds here.", flavorText: "Fresh, moist, and perfect for growing. Hard to come by in the caves.")]
[Serializable]
public class Soil : Tile {
  public Soil(Vector2Int pos) : base(pos) { }
}

[ObjectInfo("water_0", description: "Walk into to collect.", flavorText: "Water water everywhere...")]
[Serializable]
public class Water : Tile, IActorEnterHandler {
  public Water(Vector2Int pos) : base(pos) {
  }

  public void Collect() {
    GameModel.main.player.water += MyRandom.Range(105, 120);
    floor.Put(new Ground(pos));
  }

  public void HandleActorEnter(Actor who) {
    if (who == GameModel.main.player) {
      Collect();
    }
  }
}