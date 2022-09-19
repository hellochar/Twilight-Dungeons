using UnityEngine;
using UnityEngine.EventSystems;

public class WallController : TileController {
  public override void Start() {
    base.Start();
    MaybeShowFade("fade-below", Vector2Int.down);
    MaybeShowFade("fade-left", Vector2Int.left);
    MaybeShowFade("fade-right", Vector2Int.right);
    MaybeShowFade("fade-up", Vector2Int.up);
  }

  private void MaybeShowFade(string name, Vector2Int offset) {
    var fade = transform.Find(name).gameObject;
    var newPos = tile.pos + offset;
    var floor = tile.floor ?? GameModel.main.currentFloor;
    var shouldDestroyFade = !floor.InBounds(newPos) || floor.tiles[newPos] is Wall;
    if (shouldDestroyFade) {
      Destroy(fade);
    }
  }

#if experimental_3x3soil
  public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    if (tile.floor.depth == 0) {
      if (tile.visibility != TileVisiblity.Unexplored) {
        return new SetTasksPlayerInteraction(
          new MoveNextToTargetTask(GameModel.main.player, tile.pos),
          new GenericPlayerTask(GameModel.main.player, () => {
            ((Wall)tile).CarveAway();
          })
        );
      }
    }
    return base.GetPlayerInteraction(pointerEventData);
  }
#endif
}
