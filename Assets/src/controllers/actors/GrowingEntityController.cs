using UnityEngine;

public class GrowingEntityController : BodyController {
  private SpriteRenderer spriteRenderer;
  public GrowingEntity growingEntity => (GrowingEntity) body;

  Color initialColor;

  public override void Start() {
    base.Start();
    spriteRenderer = sprite.GetComponent<SpriteRenderer>();
    var prefab = FloorController.GetEntityPrefab(growingEntity.inner);
    var prefabSpriteRenderer = prefab.GetComponentInChildren<SpriteRenderer>();
    if (prefabSpriteRenderer != null) {
      spriteRenderer.sprite = prefabSpriteRenderer.sprite;
    }
    initialColor = spriteRenderer.color;
  }

  void Update() {
    spriteRenderer.color = Color.Lerp(
      initialColor,
      new Color(initialColor.r, initialColor.g, initialColor.b, 0.25f),
      (Mathf.Sin(Time.time * 4) + 1) / 2
    );
  }
}