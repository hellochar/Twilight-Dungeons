using System.Linq;
using UnityEngine;

public class BerryBush : Plant {
  public override int maxWater => 4;
  class Mature : PlantStage {
    public int numBerries = 3;

    public override float StepTime => 125;

    public override void Step() {
      numBerries += 3;
    }

    public override string getUIText() => $"Contains {numBerries} berries.\n\nGrows 3 berries in {plant.timeNextAction - plant.age} turns.";
  }

  public BerryBush(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Young();
    stage.NextStage.NextStage = new Mature();
  }

  public override Inventory HarvestRewards() {
    if (stage is Mature mature) {
      var stacks = mature.numBerries;
      if (stacks > 0) {
        var wantedStacks = stacks;
        mature.numBerries = 0;
        return new Inventory(new ItemBerries(wantedStacks));
      }
    }
    return null;
  }

  public override Inventory CullRewards() {
    if (stage is Mature) {
      return new Inventory(new ItemSeed(typeof(BerryBush)), new ItemSeed(typeof(BerryBush)));
    } else {
      return new Inventory(new ItemSeed(typeof(BerryBush)));
    }
  }
}
