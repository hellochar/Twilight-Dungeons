using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "When grown, you can Harvest this into useful items.", flavorText: "This species usually takes months to mature above ground, but the strange twilight of the caves propels plant growth.")]
public class BerryBush : Plant {
  [Serializable]
  class Mature : MaturePlantStage {
    public override void BindTo(Plant plant) {
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(BerryBush), 2),
        new ItemRedberry(2),
        new ItemStick()
      ));
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(BerryBush)),
        new ItemRedberry(2),
        new ItemBarkShield(),
        new ItemStick()
      ));
      harvestOptions.Add(new Inventory(new ItemCharmBerry(3)));
      base.BindTo(plant);
    }
  }

  public BerryBush(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Mature();
  }
}
