using System.Collections.Generic;
using UnityEngine;

public class VibrantIvyController : GrassController {
  public VibrantIvy ivy => (VibrantIvy) grass;

  public GameObject up, right, down, left;
  Animator[] animators => GetComponentsInChildren<Animator>();
  private List<GameObject> sprites;

  // Start is called before the first frame update
  public override void Start() {
    base.Start();
    sprites = new List<GameObject>() { up, right, down, left };
    MaybeDeleteSprite(up, Vector2Int.up);
    MaybeDeleteSprite(right, Vector2Int.right);
    MaybeDeleteSprite(down, Vector2Int.down);
    MaybeDeleteSprite(left, Vector2Int.left);
    // animators = GetComponentsInChildren<Animator>();
    var normalizedTime = (ivy.pos.x * 0.2f + ivy.pos.y * 0.13f) % 1;
    foreach (var animator in animators) {
      animator.Play("Breathing", 0, normalizedTime);
    }
  }

  public void Update() {
    if (ivy.Stacks < sprites.Count) {
      // 0 stacks will already be handled by the grasscontroller adding FTD to this GameObject
      if (ivy.Stacks > 0) {
        var sprite0 = sprites[0];
        sprites.RemoveAt(0);
        sprite0.AddComponent<FadeThenDestroy>();
      }
    }
  }

  private void MaybeDeleteSprite(GameObject go, Vector2Int offset) {
    var newPos = ivy.pos + offset;
    var shouldDestroySprite = !(ivy.floor.tiles[newPos] is Wall);
    if (shouldDestroySprite) {
      Destroy(go);
      sprites.Remove(go);
    }
  }
}
