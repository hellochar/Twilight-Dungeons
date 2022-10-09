using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo("cloner", description: "Duplicates the ItemGrass put in it.")]
public class Cloner : Station, IDaySteppable {
  public override int maxDurability => 9;
  // public override bool isActive => NearbyGrasses.Any() && NearbyGrounds.Any();
  public override bool isActive => itemGrass != null;
  public ItemGrass itemGrass => inventory[0] as ItemGrass;
  
  public Cloner(Vector2Int pos) : base(pos) {
    inventory.allowDragAndDrop = true;
  }

  // private IEnumerable<Tile> NearbyGrounds => floor.GetAdjacentTiles(pos).Where(t => t.grass == null);
  // private IEnumerable<Grass> NearbyGrasses => floor.GetAdjacentTiles(pos).Select(t => t.grass).Where(g => g != null);
  public void StepDay() {
    if (itemGrass != null) {
      floor.Put(new ItemOnGround(pos, new ItemGrass(itemGrass.grassType, 1), pos));
      this.ReduceDurability();
    }
    // var nearbyGroundToAddGrass = Util.RandomPick(NearbyGrounds);
    // // there's nothing left
    // if (nearbyGroundToAddGrass == null) {
    //   KillSelf();
    //   return;
    // }
    // var nearbyGrass = Util.RandomPick(NearbyGrasses);
    // if (nearbyGrass != null) {
    //   var constructor = nearbyGrass.GetType().GetConstructor(new Type[1] { typeof(Vector2Int) });
    //   var newGrass = (Grass)constructor.Invoke(new object[] { nearbyGroundToAddGrass.pos });
    //   floor.Put(newGrass);
    // }
  }
}