using UnityEngine;

public class Wildwood : Plant {
  class Mature : PlantStage {
    public override void Step() {}
    public override string getUIText() => $"Ready to harvest.";
  }

  public Wildwood(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Sapling();
    stage.NextStage.NextStage = new Mature();
  }

  public override void Harvest() {
    if (stage is Mature) {
      stage = new Sapling();
      stage.NextStage = new Mature();
      Player player = GameModel.main.player;
      player.inventory.AddItem(new ItemStick());
      player.inventory.AddItem(new ItemStick());
      player.inventory.AddItem(new ItemStick());
    }
  }

  public override void Cull() {
    Player player = GameModel.main.player;
    player.inventory.AddItem(new ItemSeed(typeof(Wildwood)));
    Harvest();
    Kill();
  }
}
