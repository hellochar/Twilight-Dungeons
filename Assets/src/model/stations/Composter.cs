using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo("composter", description: "Converts an item into Organic Matter.")]
public class Composter : Station, IDaySteppable, IInteractableInventory {
  public override int maxDurability => 9;
  public override bool isActive => inventory[0] != null;
  public Composter(Vector2Int pos) : base(pos) {
    inventory.allowDragAndDrop = true;
  }

  [PlayerAction]
  public void Compost() {
    if (inventory[0] != null) {
      var item = inventory[0];
      item.Destroy();
      int numOrganicMatters = YieldContributionUtils.GetCost(item) / 2 * item.stacks;
      for (int i = 0; i < numOrganicMatters; i++) {
        floor.Put(new ItemOnGround(pos, new ItemOrganicMatter()));
      }
    }
  }

  public void StepDay() {
    // var nearbyGroundToTurnIntoSoil = Util.RandomPick(NearbySoilableGrounds);
    // // there's nothing left
    // if (nearbyGroundToTurnIntoSoil == null) {
    //   KillSelf();
    //   return;
    // }
    // // var nearbyItemToConsume = Util.RandomPick(NearbyItems);
    // // if (nearbyItemToConsume != null) {
    // //   floor.Put(new Soil(nearbyGroundToTurnIntoSoil.pos));
    // //   nearbyItemToConsume.Kill(this);
    // // }
    // if (inventory[0] != null) {
    //   floor.Put(new Soil(nearbyGroundToTurnIntoSoil.pos));
    //   inventory[0].Destroy();
    // }
  }
}
