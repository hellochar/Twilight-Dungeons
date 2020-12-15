using UnityEngine;

public class Wildwood : Plant {
  class Mature : PlantStage {
    public override float StepTime => 99999;
    public override float Step() {
      return 99999;
    }
    public override string getUIText() => $"Ready to harvest.";
  }

  public Wildwood(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Sapling();
    stage.NextStage.NextStage = new Mature();
  }

  public override void Harvest() {
    base.Harvest();
    stage = new Sapling();
    stage.NextStage = new Mature();
  }

  public override Item[] HarvestRewards() {
    if (stage is Mature) {
      return new Item[] { new ItemStick(), new ItemStick(), new ItemStick() };
    }
    return null;
  }

  public override Item[] CullRewards() {
    return new Item[] { new ItemSeed(typeof(Wildwood)) }; 
  }
}
