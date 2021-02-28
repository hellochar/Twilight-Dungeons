using UnityEngine;

public class WallController : TileController {
  public override void Start() {
    base.Start();
    NewMethod("fade-below", Vector2Int.down);
    NewMethod("fade-left", Vector2Int.left);
    NewMethod("fade-right", Vector2Int.right);
    NewMethod("fade-up", Vector2Int.up);
  }

  private void NewMethod(string name, Vector2Int offset) {
    var fade = transform.Find(name).gameObject;
    var newPos = tile.pos + offset;
    var shouldDestroyFade = !tile.floor.InBounds(newPos) || tile.floor.tiles[newPos] is Wall;
    if (shouldDestroyFade) {
      Destroy(fade);
    }
  }
}
