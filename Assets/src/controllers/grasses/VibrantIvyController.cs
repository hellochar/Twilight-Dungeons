using UnityEngine;

public class VibrantIvyController : GrassController {
  public VibrantIvy ivy => (VibrantIvy) grass;

  public GameObject up, right, down, left;
  Animator[] animators => GetComponentsInChildren<Animator>();

  // Start is called before the first frame update
  public override void Start() {
    base.Start();
    MaybeShowSprite(up, Vector2Int.up);
    MaybeShowSprite(right, Vector2Int.right);
    MaybeShowSprite(down, Vector2Int.down);
    MaybeShowSprite(left, Vector2Int.left);
    // animators = GetComponentsInChildren<Animator>();
    var normalizedTime = (ivy.pos.x * 0.2f + ivy.pos.y * 0.13f) % 1;
    foreach (var animator in animators) {
      animator.Play("Breathing", 0, normalizedTime);
    }
  }

  private void MaybeShowSprite(GameObject go, Vector2Int offset) {
    var newPos = ivy.pos + offset;
    var shouldDestroySprite = !(ivy.floor.tiles[newPos] is Wall);
    if (shouldDestroySprite) {
      Destroy(go);
    }
  }
}
