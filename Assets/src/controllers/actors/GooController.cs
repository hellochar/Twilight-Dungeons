using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ActorController))]
public class GooController : MonoBehaviour {
  public Sprite full, half, quarter, eighth;
  private SpriteRenderer spriteRenderer;
  private ActorController actorController;
  public Goo goo => (Goo) actorController.actor;

  public void Start() {
    actorController = GetComponent<ActorController>();
    spriteRenderer = actorController.sprite.GetComponent<SpriteRenderer>();
    Update();
  }

  void Update() {
    if (goo.hp <= goo.maxHp / 8f) {
      spriteRenderer.sprite = eighth;
    } else if (goo.hp <= goo.maxHp / 4f) {
      spriteRenderer.sprite = quarter;
    } else if (goo.hp <= goo.maxHp / 2f) {
      spriteRenderer.sprite = half;
    } else {
      spriteRenderer.sprite = full;
    }
  }
}
