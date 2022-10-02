using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo("driller", description: "Destroys nearby Walls and then removes itself.")]
internal class Driller : Station, IDaySteppable {
  public Driller(Vector2Int pos) : base(pos) {
  }

  public override int maxDurability => 9;

  public override bool isActive => NearbyWalls().Any();

  public void StepDay() {
    var nearbyWall = Util.RandomPick(NearbyWalls());
    if (nearbyWall != null) {
      floor.Put(new Ground(nearbyWall.pos));
    } else {
      // we're done
      KillSelf();
    }
  }

  private IEnumerable<Tile> NearbyWalls() {
    return floor.GetAdjacentTiles(pos).Where(t => t is Wall);
  }
}