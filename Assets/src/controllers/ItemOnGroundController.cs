using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Connects an ItemOnGround GameObject (this.gameObject) to an ItemOnGround entity.
/// </summary>
public class ItemOnGroundController : MonoBehaviour, IEntityController, IEntityClickedHandler {
  public ItemOnGround itemOnGround;
  private SpriteRenderer spriteRenderer;

  void Start() {
    this.transform.position = Util.withZ(this.itemOnGround.pos, this.transform.position.z);
    spriteRenderer = transform.Find("Sprite").GetComponent<SpriteRenderer>();
    // set the sprite
    var wantedSprite = ObjectInfo.GetSpriteFor(itemOnGround.item);
    spriteRenderer.sprite = wantedSprite;
    if (itemOnGround.start != null) {
      StartCoroutine(BounceOntoGroundAnimation(transform, itemOnGround.start.Value, transform.position));
    }
  }

  public static IEnumerator BounceOntoGroundAnimation(Transform transform, Vector2Int startPos, Vector3 end) {
    Vector3 start = Util.withZ(startPos, end.z);
    transform.position = start;
    var delay = UnityEngine.Random.Range(0, 0.01f);
    yield return new WaitForSeconds(delay);
    float startTime = Time.time;
    var t = 0f;
    do {
      yield return new WaitForEndOfFrame();
      t = (Time.time - startTime) / 0.5f;
      transform.position = Vector3.Lerp(start, end, EasingFunctions.EaseOutCubic(0, 1, t)) + new Vector3(0, 0, t * 3 * (1 - EasingFunctions.EaseInBounce(0, 1, t)));
    } while (t <= 1);
    transform.position = end;
  }

  public void PointerClick(PointerEventData pointerEventData) {
    if (itemOnGround.isVisible) {
      GameModel.main.player.task = new MoveToTargetTask(GameModel.main.player, itemOnGround.pos);
    }
  }
}