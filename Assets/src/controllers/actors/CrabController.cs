using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CrabController : ActorController {
  public Crab crab => (Crab) actor;
  public Sprite right, rightMid, mid, leftMid, left;
  private SpriteRenderer spriteRenderer;

  public override void Start() {
    base.Start();
    spriteRenderer = sprite.GetComponent<SpriteRenderer>();
    crab.OnDirectionChanged += HandleDirectionChanged;
    spriteRenderer.sprite = crab.dx == 1 ? right : left;
  }

  private void HandleDirectionChanged() {
    if (crab.dx == -1) {
      // going from right to left
      StartCoroutine(Transitions.SpriteSwap(spriteRenderer, 0.25f, right, rightMid, mid, leftMid, left));
    } else {
      // left to right
      StartCoroutine(Transitions.SpriteSwap(spriteRenderer, 0.25f, left, leftMid, mid, rightMid, right));
    }
  }
}
