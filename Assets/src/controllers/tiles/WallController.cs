using UnityEngine;

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
    var shouldDestroyFade = !tile.floor.InBounds(newPos) || tile.floor.tiles[newPos] is Wall;
    if (shouldDestroyFade) {
      Destroy(fade);
    }
  }
}
