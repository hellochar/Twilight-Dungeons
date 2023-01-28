using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public abstract class Tile : Entity {
  public TileVisiblity visibility = TileVisiblity.Unexplored;
  public bool isExplored => !IsDead && visibility != TileVisiblity.Unexplored;

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
    /// set correct visibility when the tile is dynamically added
    var player = GameModel.main?.player;
    if (player != null) {
      if (floor == player.floor) {
        visibility = floor.TestVisibility(player.pos, pos);
      }
    }
  }

  /// 0.0 means unwalkable.
  /// weight 1 is "normal" weight.
  public virtual float GetPathfindingWeight() => body != null ? 0 : BasePathfindingWeight();

  internal void BodyLeft(Body body) {
    if (body is Actor actor) {
      GameModel.main.EnqueueEvent(() => {
        foreach (var handler in this.Of<IActorLeaveHandler>()) {
          if (floor.depth == 0 && handler is Grass) {
            continue;
          }
          handler.HandleActorLeave(actor);
        }
      });
    }
  }

  internal void BodyEntered(Body body) {
    if (body is Actor actor) {
      GameModel.main.EnqueueEvent(() => {
        foreach (var handler in this.Of<IActorEnterHandler>()) {
          if (floor.depth == 0 && handler is Grass) {
            continue;
          }
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

  public virtual bool ObstructsExploration() {
    return body is IBlocksExploration || grass is IBlocksExploration;
  }

  internal bool CanBeOccupied() {
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
  Unexplored = 0, Visible = 2, Explored = 1
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
    Popups.CreateStandard(
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

  public void Soften() {
    var player = GameModel.main.player;
    player.UseResourcesOrThrow(water: 11);
    floor.Put(new Ground(pos));
  }

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
#if experimental_actionpoints
public class Wall : Tile, IBlocksExploration {
#else
public class Wall : Tile {
#endif
  public Wall(Vector2Int pos) : base(pos) { }
  public override float BasePathfindingWeight() {
    return 0;
  }

#if experimental_actionpoints
  protected override void HandleLeaveFloor() {
    if (floor is HomeFloor f) {
      f.root.ExtendToEncompass(new Room(pos - Vector2Int.one, pos + Vector2Int.one));
    }
  }

  // [PlayerAction]
  // public void CarveAway() {
  //   var player = GameModel.main.player;
  //   player.UseActionPointOrThrow();
  //   floor.Put(new Ground(tile.pos));
  // }
#endif
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

// [Serializable]
// [ObjectInfo(description: "Go back home.")]
// public class Upstairs : Tile, IActorEnterHandler {
//   /// <summary>Where the player will be after taking the Downstairs connected to this tile.</summary>
//   public Vector2Int landing => pos + Vector2Int.right;
//   public Upstairs(Vector2Int pos) : base(pos) {}

//   protected override void HandleEnterFloor() {
//     base.HandleEnterFloor();
//     grass?.Kill(this);
//   }

//   public void TryGoHome() {
//     if (floor.EnemiesLeft() == 0) {
//       Serializer.SaveMainToCheckpoint();
//       GameModel.main.PutPlayerAt(0);
//     } else {
//       GameObject.Find("Enemies Left")?.AddComponent<PulseAnimation>()?.Larger();
//     }
//   }

//   public void HandleActorEnter(Actor who) {
//     if (who is Player) {
//       GameModel.main.EnqueueEvent(TryGoHome);
//     }
//   }
// }

[Serializable]
[ObjectInfo(description: "Go deeper into the dungeon.")]
public class Downstairs : Tile {
  /// <summary>Where the player will be after taking the Upstairs connected to this tile.</summary>
  public Vector2Int landing => pos + Vector2Int.left;
  public Downstairs(Vector2Int pos) : base(pos) {}

  // protected override void HandleEnterFloor() {
  //   base.HandleEnterFloor();
  //   grass?.Kill(this);
  // }

  // public virtual void HandleActorEnter(Actor who) {
  //   if (who is Player) {
  //     GameModel.main.EnqueueEvent(TryGoDownstairs);
  //   }
  // }
}

[Serializable]
[ObjectInfo(description: "Jump into the dungeon.")]
public class Pit : Piece {
  public Pit(Vector2Int pos) : base(pos) { }

  public static Vector2Int[] Shape2x2 = new Vector2Int[] { Vector2Int.zero, new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, 1) };
  public override Vector2Int[] shape => Shape2x2;

  [PlayerAction]
  public void Delve() {
    Serializer.SaveMainToCheckpoint();
    floor.PlayerGoDownstairs();
  }

  // public override string description => $"Delve into depth {GameModel.main.cave.depth}.";

  public override string description {
    get {
      var entityTypes = GameModel.main.cave
        .bodies.Where(b => !(b is IHideInSidebar)).GroupBy(b => b.GetType()).Select(grouping => grouping.First()).Cast<Entity>()
        .Concat(
          GameModel.main.cave
          .grasses.GroupBy(g => g.GetType()).Select(grouping => grouping.First())
        );
      return $"You peer into depth {GameModel.main.cave.depth} and see " + String.Join(", ", entityTypes.Select(e => e.displayName)) + ".";
    }
  }
}

[Serializable]
[ObjectInfo(description: "A way back home!")]
public class Teleporter : Downstairs {
  public Teleporter(Vector2Int pos) : base(pos) {
  }

  internal void GoHome() {
    floor.PlayerGoHome();
  }

  // public override void HandleActorEnter(Actor who) {
  //   // no op
  // }
}

[ObjectInfo("water_0", description: "Walk into to collect.", flavorText: "Water water everywhere...")]
[Serializable]
public class Water : Tile, IActorEnterHandler {
  public Water(Vector2Int pos) : base(pos) {
  }

  public void Collect() {
    GameModel.main.player.water += MyRandom.Range(55, 65);
    if (floor is HomeFloor) {
      floor.Put(new HomeGround(pos));
    } else {
      floor.Put(new Ground(pos));
    }
  }

  public void HandleActorEnter(Actor who) {
    if (who == GameModel.main.player) {
      Collect();
    }
  }
}

public static class TileExtensions {
  public static bool CanPlaceShape(this Tile tile, Vector2Int[] shape) {
    var floor = tile.floor;
    foreach (Vector2Int offset in shape) {
      var newPos = tile.pos + offset;
      if (!floor.InBounds(newPos)) {
        return false;
      }
      if (!floor.tiles[newPos].CanBeOccupied()) {
        return false;
      }
    }
    return true;
  }
}