using UnityEngine;
using UnityEngine.EventSystems;

public class FungalWallController : TileController {
  FungalWall fungalWall => (FungalWall) tile;

  public override void HandleInteracted(PointerEventData pointerEventData) {
    if (tile.visibility != TileVisiblity.Unexplored) {
      GameModel.main.player.SetTasks(
        new MoveToTargetTask(GameModel.main.player, tile.pos),
        new GenericTask(GameModel.main.player, fungalWall.Clear)
      );
    }
  }
}
