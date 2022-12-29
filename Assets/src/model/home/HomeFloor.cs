using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
public class HomeFloor : Floor {
  public StaticEntityGrid<Soil> soils;
  public StaticEntityGrid<Piece> pieces;
  public IEnumerable<Plant> plants => pieces.Where(b => b is Plant).Cast<Plant>();

  public HomeFloor(int width, int height) : base(0, width, height) {
    soils = new StaticEntityGrid<Soil>(this);
    pieces = new StaticEntityGrid<Piece>(this);
  }

  public override void Put(Entity entity) {
#if experimental_survivalhomefloor
    if (entity is Tile t) {
      t.visibility = TileVisiblity.Explored;
    }
#endif
    if (entity is Soil s) {
      soils.Put(s);
    } else if (entity is Piece p) {
      pieces.Put(p);
    }
    base.Put(entity);
  }

  public override void Remove(Entity entity) {
    if (entity is Soil s) {
      soils.Remove(s);
    } else if (entity is Piece p) {
      pieces.Remove(p);
    }
    base.Remove(entity);
  }

#if !experimental_survivalhomefloor
  protected override TileVisiblity RecomputeVisibilityFor(Tile t) {
    if (root.Contains(t.pos)) {
      return base.RecomputeVisibilityFor(t);
    }
    return t.visibility;
  }
#endif

  public void AddWallsOutsideRoot() {
    var rootInset = new Room(root.min + Vector2Int.one, root.max - Vector2Int.one);
    foreach(var pos in this.EnumerateFloor()) {
      if (!rootInset.Contains(pos) && tiles[pos].CanBeOccupied()) {
        Put(new Wall(pos));
      }
    }
  }

  public void AddThickBrushOutsideRoot() {
    var rootInset = new Room(root.min + Vector2Int.one, root.max - Vector2Int.one);
    foreach(var pos in this.EnumerateFloor()) {
      if (!rootInset.Contains(pos) && tiles[pos].CanBeOccupied() && tiles[pos] is Ground && tiles[pos].item == null) {
        Put(new ThickBrush(pos));
      }
    }
  }

  public bool CanPlacePiece(Entity e, Tile tile) {
    foreach (Vector2Int offset in e.shape) {
      var newPos = tile.pos + offset;
      if (!InBounds(newPos)) {
        return false;
      }
      if (!tiles[newPos].CanBeOccupied()) {
        return false;
      }
    }
    return true;
  }
}

[Serializable]
public class ExpandingHomeFloor : HomeFloor {
  public static ExpandingHomeFloor generate(Vector2Int startSize, int numFloors) {
    var finalSize = startSize + 2 * Vector2Int.one * numFloors;
    ExpandingHomeFloor floor = new ExpandingHomeFloor(finalSize.x, finalSize.y);
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Chasm(p));
    }

    // put a pit at the center
    var center = floor.center;
    var min = floor.center - startSize / 2;
    Room room0 = new Room(
      min,
      min + startSize - Vector2Int.one
    );
    floor.rooms = new List<Room>() { room0 };
    floor.root = room0;
    foreach (var p in floor.EnumerateRoom(room0)) {
      floor.Put(new HomeGround(p));
    }

    floor.startPos = new Vector2Int(room0.min.x, room0.center.y);
    floor.Put(new Pit(room0.center));
    Encounters.AddWater(floor, room0);

    // show chasm edges
    room0.max += Vector2Int.one;
    room0.min -= Vector2Int.one;

    return floor;
  }

  public ExpandingHomeFloor(int width, int height) : base(width, height) { }
}

[Serializable]
[ObjectInfo("z-bush", description: "Cut away to make more space.")]
public class ThickBrush : Destructible, IBlocksVision {
  public ThickBrush(Vector2Int pos) : base(pos, 0) {
  }

  protected override void HandleLeaveFloor() {
    if (floor is HomeFloor f) {
      f.root.ExtendToEncompass(new Room(pos - Vector2Int.one, pos + Vector2Int.one));
      f.RecomputeVisibility();
    }
  }

  [PlayerAction]
  public void Cut() {
    // cut this 3x3 area
    var player = GameModel.main.player;
    player.UseActionPointOrThrow();
    // var xStart = (pos.x / 3) * 3;
    // var yStart = (pos.y / 3) * 3;
    // foreach(var p in floor.EnumerateRectangle(new Vector2Int(xStart, yStart), new Vector2Int(xStart + 3, yStart + 3))) {
    foreach (var p in floor.GetAdjacentTiles(pos).Select(t => t.pos)) {
      var brush = player.floor.bodies[p] as ThickBrush;
      if (brush != null) {
        brush.Kill(player);
      }
    }
    player.floor.Put(new OrganicMatterOnGround(pos));
    player.floor.RecomputeVisibility();
  }
}

public interface IDaySteppable {
  void StepDay();
}
