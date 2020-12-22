using UnityEngine;

public class Wildwood : Plant {
  public override int maxWater => 5;
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

  public override Inventory HarvestRewards() {
    if (stage is Mature) {
      return new Inventory(new ItemStick(), new ItemStick(), new ItemWildwoodLeaf(3), new ItemWildwoodWreath());
    }
    return null;
  }

  public override Inventory CullRewards() {
    return new Inventory(new ItemSeed(typeof(Wildwood))); 
  }
}
