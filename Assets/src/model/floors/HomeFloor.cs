using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
public class HomeFloor : Floor {
  public HomeFloor(int width, int height) : base(0, width, height) {
  }

  public IEnumerable<Plant> plants => bodies.Where(b => b is Plant).Cast<Plant>();

#if experimental_survivalhomefloor
  public override void Put(Entity entity) {
    if (entity is Tile t) {
      t.visibility = TileVisiblity.Explored;
    }
    base.Put(entity);
  }
#endif

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
      if (!rootInset.Contains(pos) && tiles[pos].CanBeOccupied()) {
        Put(new ThickBrush(pos));
      }
    }
  }
}

public interface IDaySteppable {
  void StepDay();
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
    var room = this.room;
    if (room != null) {
      // var xStart = (pos.x / 3) * 3;
      // var yStart = (pos.y / 3) * 3;
      // foreach(var p in floor.EnumerateRectangle(new Vector2Int(xStart, yStart), new Vector2Int(xStart + 3, yStart + 3))) {
      foreach (var p in floor.GetAdjacentTiles(pos).Select(t => t.pos)) {
        var brush = player.floor.bodies[p] as ThickBrush;
        if (brush != null) {
          brush.Kill(player);
        }
      }
    }
    player.floor.RecomputeVisibility();
  }
}