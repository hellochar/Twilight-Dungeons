using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo("composter", description: "Converts an item into Organic Matter.")]
public class Composter : Station, IInteractableInventory {
  public override int maxDurability => 9;
  public override bool isActive => inventory[0] != null;
  public Composter(Vector2Int pos) : base(pos) {
    inventory.allowDragAndDrop = true;
  }

  [PlayerAction]
  public void Compost() {
    if (inventory[0] != null) {
      var item = inventory[0];
      int numOrganicMatters = YieldContributionUtils.GetCost(item) / 2;
      for (int i = 0; i < numOrganicMatters; i++) {
        floor.Put(new OrganicMatterOnGround(pos));
      }
      item.stacks--;
    }
  }
}
