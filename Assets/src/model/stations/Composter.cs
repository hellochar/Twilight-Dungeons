using System;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo("composter", description: "Converts one nearby item into a Soil each day.")]
public class Composter : Station, IDaySteppable {
  public override int maxDurability => 9;
  public Composter(Vector2Int pos) : base(pos) { }

  public void StepDay() {
    var nearbyGroundToTurnIntoSoil = Util.RandomPick(
      floor.GetAdjacentTiles(pos).Where(t => t is Ground && !(t is Soil))
    );
    // there's nothing left
    if (nearbyGroundToTurnIntoSoil == null) {
      KillSelf();
      return;
    }
    var nearbyItemToConsume = Util.RandomPick(
      floor.GetAdjacentTiles(pos).Select(t => t.item).Where(i => i != null)
    );
    if (nearbyItemToConsume != null) {
      floor.Put(new Soil(nearbyGroundToTurnIntoSoil.pos));
    }
  }
}
