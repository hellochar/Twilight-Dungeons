using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Connects an ItemOnGround GameObject (this.gameObject) to an ItemOnGround entity.
/// </summary>
public class ItemOnGroundController : MonoBehaviour, IEntityController {
  public ItemOnGround itemOnGround;
  private SpriteRenderer sprite;

  void Start() {
    this.transform.position = Util.withZ(this.itemOnGround.pos, this.transform.position.z);
    sprite = transform.Find("Sprite").GetComponent<SpriteRenderer>();
    // set the sprite
    var wantedSprite = ObjectInfo.GetSpriteFor(itemOnGround.item);
    sprite.sprite = wantedSprite;
  }
}
