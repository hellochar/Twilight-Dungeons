using UnityEngine;

public class GrowingEntityController : StationController {
  private SpriteRenderer spriteRenderer;
  public GrowingEntity growingEntity => (GrowingEntity) body;

  public override void Start() {
    base.Start();
    spriteRenderer = sprite.GetComponent<SpriteRenderer>();
    var prefab = FloorController.GetEntityPrefab(growingEntity.inner);
    var prefabSpriteRenderer = prefab.GetComponentInChildren<SpriteRenderer>();
    if (prefabSpriteRenderer != null) {
      spriteRenderer.sprite = prefabSpriteRenderer.sprite;
    }
  }
}