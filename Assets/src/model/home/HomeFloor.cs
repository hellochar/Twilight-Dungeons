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
