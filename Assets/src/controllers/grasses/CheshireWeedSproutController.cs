using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class CheshireWeedSproutController : GrassController, IEntityControllerRemoveOverride {
  public Sprite[] stages;
  public CheshireWeedSprout sprout => (CheshireWeedSprout) grass;

  public override void Start() {
    base.Start();
    Update();
  }

  void Update() {
    int spriteIndex = Mathf.Clamp((int)sprout.age, 0, stages.Length - 1);
    var targetSprite = stages[spriteIndex];
    if (sr.sprite != targetSprite) {
      sr.sprite = targetSprite;
    }
  }

  public void OverrideRemoved() {
    Destroy(gameObject);
  }
}