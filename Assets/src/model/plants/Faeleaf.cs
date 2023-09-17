using System;
using UnityEngine;

[Serializable]
public class Faeleaf : Plant {
  public static int waterCost => 50;
  [Serializable]
  class Mature : MaturePlantStage {
    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Faeleaf), 3)
      ));
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Faeleaf), 1),
        // new ItemSnakeVenom(12)
        new ItemCoralChunk(3)
      ));
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Faeleaf), 1),
        new ItemBirdWings(6)
      ));
      harvestOptions.Add(new Inventory(
        new ItemWallflowerTendril()
      ));
    }
  }
  public Faeleaf(Vector2Int pos) : base(pos, new Seed(2)) {
    stage.NextStage = new Mature();
  }
}