using System;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo("composter", description: "Converts a Grass into Organic Matter.")]
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
      if (!(item is ItemGrass)) {
        throw new CannotPerformActionException("Must put in a Grass.");
      }
      floor.Put(new OrganicMatterOnGround(pos));
      item.stacks--;
    }
  }
}
