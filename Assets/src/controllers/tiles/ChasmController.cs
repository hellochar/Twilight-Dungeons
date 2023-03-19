using UnityEngine;
using UnityEngine.EventSystems;

public class ChasmController : TileController {
  public GameObject fade;
  public GameObject borderTop, borderLeft, borderBottom, borderRight;
  public override void Start() {
    base.Start();
    var above = tile.pos + Vector2Int.up;
    var shouldDestroy = !tile.floor.InBounds(above) || tile.floor.tiles[above] is Chasm;
    if (shouldDestroy) {
      Destroy(fade);
    }
    MaybeShowBorder(borderTop, Vector2Int.up);
    MaybeShowBorder(borderLeft, Vector2Int.left);
    MaybeShowBorder(borderBottom, Vector2Int.down);
    MaybeShowBorder(borderRight, Vector2Int.right);

    if (/*tile.floor is HomeFloor && */tile is Mist mist) {
      var ps = GetComponent<ParticleSystem>();
      var main = ps.main;
      main.startLifetimeMultiplier = mist.depth;
    }
  }

  private void MaybeShowBorder(GameObject fade, Vector2Int offset) {
    var newPos = tile.pos + offset;
    var shouldDestroy = !tile.floor.InBounds(newPos) || tile.floor.tiles[newPos] is Chasm;
    if (shouldDestroy) {
      Destroy(fade);
    }
  }

  public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    // I'm lazy and don't want to create a new controller
    if (/*tile.floor is HomeFloor && */tile is Mist) {
      if (tile.visibility != TileVisiblity.Unexplored) {
        return new SetTasksPlayerInteraction(
          new MoveNextToTargetTask(GameModel.main.player, tile.pos),
          new ShowInteractPopupTask(GameModel.main.player, tile)
        );
      }
    }
    return base.GetPlayerInteraction(pointerEventData);
  }
}
