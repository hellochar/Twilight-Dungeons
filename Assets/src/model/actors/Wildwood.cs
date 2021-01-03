using UnityEngine;

public class Wildwood : Plant {
  public override int maxWater => 5;
  class Mature : PlantStage {
    public override float StepTime => 99999;
    public override void Step() { }
    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      plant.inventory.AddItem(new ItemStick());
      plant.inventory.AddItem(new ItemWildwoodLeaf(3));
      plant.inventory.AddItem(new ItemWildwoodWreath());
    }
    public override string getUIText() => $"Ready to harvest.";
  }

  public Wildwood(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Sapling();
    stage.NextStage.NextStage = new Mature();
  }
}
