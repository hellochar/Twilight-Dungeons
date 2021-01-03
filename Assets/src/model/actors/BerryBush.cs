using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BerryBush : Plant {
  public override int maxWater => 4;
  class Mature : PlantStage {
    public override float StepTime => 125;

    public override void Step() {
      plant.inventory.AddItem(new ItemRedberry(3));
    }

    public override void BindTo(Plant plant) {
      plant.inventory.AddItem(new ItemRedberry(3));
      plant.inventory.AddItem(new ItemBarkShield());
      plant.inventory.AddItem(new ItemSeed(typeof(BerryBush)));
      base.BindTo(plant);
    }

    public override string getUIText() => $"Grows 3 berries in {plant.timeNextAction - plant.age} turns.";
  }

  public BerryBush(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Young();
    stage.NextStage.NextStage = new Mature();
  }
}
