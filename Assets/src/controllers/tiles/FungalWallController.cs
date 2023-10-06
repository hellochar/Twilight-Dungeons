using UnityEngine;
using UnityEngine.EventSystems;

public class FungalWallController : TileController {
  FungalWall fungalWall => (FungalWall) tile;

  public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    return new SetTasksPlayerInteraction(
      new MoveToTargetTask(GameModel.main.player, tile.pos),
      new GenericTask(GameModel.main.player, fungalWall.Clear)
    );
  }
}
