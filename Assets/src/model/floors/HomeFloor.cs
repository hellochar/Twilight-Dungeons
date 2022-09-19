using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
public class HomeFloor : Floor {
  public HomeFloor(int width, int height) : base(0, width, height) {
  }

  public int actionPoints = 3;
  public int maxActionPoints = 3;

  protected override TileVisiblity RecomputeVisibilityFor(Tile t) {
    if (root.Contains(t.pos)) {
      return base.RecomputeVisibilityFor(t);
    }
    return t.visibility;
  }

  internal void AddInitialThickBrush() {
    var rootInset = new Room(root.min + Vector2Int.one, root.max - Vector2Int.one);
    foreach(var pos in this.EnumerateFloor()) {
      if (!rootInset.Contains(pos) && tiles[pos].CanBeOccupied()) {
        Put(new ThickBrush(pos));
      }
    }
  }

  internal void PlayerWentHome() {
    actionPoints = maxActionPoints;
  }
}

[Serializable]
public class ThickBrush : Destructible, IBlocksExploration {
  public ThickBrush(Vector2Int pos) : base(pos) {}

  protected override void HandleLeaveFloor() {
    if (floor is HomeFloor f) {
      f.root.ExtendToEncompass(new Room(pos - Vector2Int.one, pos + Vector2Int.one));
      f.RecomputeVisibility();
    }
  }
}