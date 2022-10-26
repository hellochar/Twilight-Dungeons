using System;

[Serializable]
[ObjectInfo("plant-matter", description: "Turn into organic matter at home.")]
public class ItemOrganicMatter : Item, IActorEnterHandler {

  [PlayerAction]
  public void Collect() {
    if (GameModel.main.currentFloor.depth == 0) {
      GameModel.main.player.organicMatter += stacks;
      stacks = 0;
    }
  }

  public void HandleActorEnter(Actor who) {
    if (who is Player) {
      Collect();
    }
  }
}