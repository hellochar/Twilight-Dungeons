using System;
using UnityEngine;

public class BerryBush : Plant {

  class Mature : PlantStage {
    public int numBerries = 1;

    public override float StepTime => 250;

    public override float Step() {
      numBerries++;
      return StepTime;
    }

    public override string getUIText() => $"Contains {numBerries} berries.";
  }

  public BerryBush(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Young();
    stage.NextStage.NextStage = new Mature();
  }

  public override void Harvest() {
    Player player = GameModel.main.player;
    var stacks = stage is Mature ? ((Mature) stage).numBerries : 0;
    if (stacks > 0) {
      var item = new ItemBerries(stacks);
      player.inventory.AddItem(item);
      ((Mature) stage).numBerries = 0;
    }
  }

  public override void Cull() {
    Player player = GameModel.main.player;
    player.inventory.AddItem(new ItemSeed(typeof(BerryBush)));
    if (stage is Mature) {
      player.inventory.AddItem(new ItemSeed(typeof(BerryBush)));
    }
    this.Harvest();
    this.Kill();
  }
}