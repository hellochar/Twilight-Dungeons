using UnityEngine;

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
  }

  private void MaybeShowBorder(GameObject fade, Vector2Int offset) {
    var newPos = tile.pos + offset;
    var shouldDestroy = !tile.floor.InBounds(newPos) || tile.floor.tiles[newPos] is Chasm;
    if (shouldDestroy) {
      Destroy(fade);
    }
  }
}
