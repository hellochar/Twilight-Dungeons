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

// A piece on the home garden.
[Serializable]
public class Piece : Entity {
  private Vector2Int _pos;
  public override Vector2Int pos {
    get => _pos;
    /// do not allow moving Pieces
    set { }
  }
  public HomeFloor home => floor as HomeFloor;
  public Soil soil => home.soils[pos];
  public int dayCreated { get; }
  public int dayAge => GameModel.main.day - dayCreated;
  public Piece(Vector2Int pos) : base() {
    _pos = pos;
    dayCreated = GameModel.main.day;
  }

  public ItemOfPiece BecomeItem() {
    if (floor != null) {
      floor.Remove(this);
    }
    return new ItemOfPiece(this);
  }
}

// A Piece that represents an Entity in the caves such as a 
// Grass or Creature
[Serializable]
public class CavePiece<T> : Piece where T : Entity {
  public CavePiece(Vector2Int pos, T type) : base(pos) {
  }
}

[Serializable]
public class HomeInventory : Inventory {
  public override bool AddItem(Item item, Entity source = null, bool expandToFit = false) {
    if (item is ItemOfPiece) {
      return base.AddItem(item, source, expandToFit);
    }
    return false;
  }
}

[Serializable]
public class ItemOfPiece : Item {
  Piece piece;

  public ItemOfPiece(Piece p) {
    this.piece = p;
  }
}