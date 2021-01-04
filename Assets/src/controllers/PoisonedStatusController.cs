using UnityEngine;

public class PoisonedStatusController : StatusController {
  public SpriteRenderer spriteRenderer;
  public Sprite[] sprites;

  PoisonedStatus poisoned => (PoisonedStatus) status;
  void Update() {
    spriteRenderer.sprite = Util.ClampGet(poisoned.stacks - 1, sprites);
  }
}
