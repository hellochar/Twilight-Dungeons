using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo("composter", description: "Converts one nearby item into a Soil each day.")]
public class Composter : Station, IDaySteppable {
  public override int maxDurability => 9;
  // public override bool isActive => NearbySoilableGrounds.Any() && NearbyItems.Any();
  public override bool isActive => NearbySoilableGrounds.Any() && inventory[0] != null;
  public Composter(Vector2Int pos) : base(pos) {
    inventory.allowDragAndDrop = true;
  }

  private IEnumerable<Tile> NearbySoilableGrounds =>
    floor.GetAdjacentTiles(pos).Where(t => t is Ground && !(t is Soil));

  private IEnumerable<ItemOnGround> NearbyItems =>
    floor.GetAdjacentTiles(pos).Select(t => t.item).Where(i => i != null);

  public void StepDay() {
    var nearbyGroundToTurnIntoSoil = Util.RandomPick(NearbySoilableGrounds);
    // there's nothing left
    if (nearbyGroundToTurnIntoSoil == null) {
      KillSelf();
      return;
    }
    // var nearbyItemToConsume = Util.RandomPick(NearbyItems);
    // if (nearbyItemToConsume != null) {
    //   floor.Put(new Soil(nearbyGroundToTurnIntoSoil.pos));
    //   nearbyItemToConsume.Kill(this);
    // }
    if (inventory[0] != null) {
      floor.Put(new Soil(nearbyGroundToTurnIntoSoil.pos));
      inventory[0].Destroy();
    }
  }
}
