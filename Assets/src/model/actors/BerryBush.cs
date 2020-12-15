using System;
using System.Linq;
using UnityEngine;

public class BerryBush : Plant {

  class Mature : PlantStage {
    public int numBerries = 3;

    public override float StepTime => 250;

    public override float Step() {
      numBerries += 3;
      return StepTime;
    }

    public override string getUIText() => $"Contains {numBerries} berries.";
  }

  public BerryBush(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Young();
    stage.NextStage.NextStage = new Mature();
  }

  public override Item[] HarvestRewards() {
    if (stage is Mature mature) {
      var stacks = mature.numBerries;
      if (stacks > 0) {
        stacks = 0;
        return new Item[] { new ItemBerries(stacks) };
      }
    }
    return null;
  }

  public override Item[] CullRewards() {
    if (stage is Mature) {
      return new Item[] { new ItemSeed(typeof(BerryBush)), new ItemSeed(typeof(BerryBush)) };
    } else {
      return new Item[] { new ItemSeed(typeof(BerryBush)) };
    }
  }
}
