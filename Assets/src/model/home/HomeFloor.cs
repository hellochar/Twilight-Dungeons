using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
public class HomeFloor : Floor {

  public HomeFloor(int width, int height) : base(0, width, height) {
  }

  public override void RecomputeVisibility() {
#if experimental_cavenetwork
    base.RecomputeVisibility();
    return;
#else
    var player = GameModel.main.player;
    if (player == null || player.floor != this) {
      return;
    }

    foreach (var tile in this.BreadthFirstSearch(player.pos, t => !t.ObstructsExploration(), mooreNeighborhood: true, includeEdge: true)) {
      if (tile != null) {
        tile.visibility = RecomputeVisibilityFor(tile);
      }
    }
#endif
  }

#if !experimental_cavenetwork
  protected override TileVisiblity RecomputeVisibilityFor(Tile t) {
    if (root.Contains(t.pos)) {
      return base.RecomputeVisibilityFor(t);
    }
    return t.visibility;
  }
#endif
}
