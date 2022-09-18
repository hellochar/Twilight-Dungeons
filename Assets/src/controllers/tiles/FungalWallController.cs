using UnityEngine;
using UnityEngine.EventSystems;

public class FungalWallController : TileController {
  FungalWall fungalWall => (FungalWall) tile;

  public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    if (tile.visibility != TileVisiblity.Unexplored) {
      return new SetTasksPlayerInteraction(
        new MoveToTargetTask(GameModel.main.player, tile.pos),
        new GenericTask(GameModel.main.player, fungalWall.Clear)
      );
    }
    return base.GetPlayerInteraction(pointerEventData);
  }
}
