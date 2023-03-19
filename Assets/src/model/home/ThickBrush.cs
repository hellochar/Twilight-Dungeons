using System;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo("z-bush", description: "Cut away to make more space.")]
public class ThickBrush : Destructible, IBlocksExploration {
  public ThickBrush(Vector2Int pos) : base(pos, 0) {
  }

#if !experimental_cavenetwork
  protected override void HandleLeaveFloor() {
    if (floor is HomeFloor f) {
      f.root.ExtendToEncompass(new Room(pos - Vector2Int.one, pos + Vector2Int.one));
      f.RecomputeVisibility();
    }
  }
#endif

  [PlayerAction]
  public void Cut() {
    // cut this 3x3 area
    var player = GameModel.main.player;
    player.UseActionPointOrThrow();
    // var xStart = (pos.x / 3) * 3;
    // var yStart = (pos.y / 3) * 3;
    // foreach(var p in floor.EnumerateRectangle(new Vector2Int(xStart, yStart), new Vector2Int(xStart + 3, yStart + 3))) {
    // foreach (var p in floor.GetAdjacentTiles(pos).Select(t => t.pos)) {
    //   var brush = player.floor.bodies[p] as ThickBrush;
    //   if (brush != null) {
    //     brush.Kill(player);
    //   }
    // }
    Kill(player);
    if (MyRandom.value < 0.1f) {
      player.floor.Put(new OrganicMatterOnGround(pos));
    }
    player.floor.RecomputeVisibility();
  }
}
