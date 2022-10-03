using UnityEngine;

public class GrowingEntityController : BodyController {
  private SpriteRenderer spriteRenderer;
  public GrowingEntity growingEntity => (GrowingEntity) body;

  Color initialColor;

  public override void Start() {
    base.Start();
    spriteRenderer = sprite.GetComponent<SpriteRenderer>();
    var innerSprite = ObjectInfo.GetSpriteFor(growingEntity.inner);
    if (innerSprite == null) {
      var prefab = FloorController.GetEntityPrefab(growingEntity.inner);
      var prefabSpriteRenderer = prefab.GetComponentInChildren<SpriteRenderer>();
      if (prefabSpriteRenderer != null) {
        innerSprite = prefabSpriteRenderer.sprite;
      }
    }
    spriteRenderer.sprite = innerSprite;
    initialColor = spriteRenderer.color;
  }

  void Update() {
    spriteRenderer.color = Color.Lerp(
      initialColor,
      new Color(initialColor.r, initialColor.g, initialColor.b, 0.0f),
      (Mathf.Sin(Time.time * 6) + 1) / 2
    );
  }
}