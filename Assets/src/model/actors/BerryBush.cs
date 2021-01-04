using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BerryBush : Plant {
  public override int maxWater => 4;
  class Mature : PlantStage {
    public override float StepTime => 999999;

    public override void Step() {}

    public override void BindTo(Plant plant) {
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(BerryBush)),
        new ItemSeed(typeof(BerryBush)),
        new ItemRedberry(3),
        new ItemStick()
      ));
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(BerryBush)),
        new ItemRedberry(3),
        new ItemBarkShield(),
        new ItemStick()
      ));
      harvestOptions.Add(new Inventory(new ItemCharmBerry(3)));
      base.BindTo(plant);
    }

    public override string getUIText() => $"Grows 3 berries in {plant.timeNextAction - plant.age} turns.";
  }

  public BerryBush(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Mature();
  }
}
