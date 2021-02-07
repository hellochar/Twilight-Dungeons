using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class VioletsController : GrassController {
  public Sprite[] flowerStages;
  public Sprite open;
  public Violets violets => (Violets) grass;
  private SpriteRenderer sr;

  public override void Start() {
    base.Start();
    sr = transform.Find("Flower").GetComponent<SpriteRenderer>();
    Update();
  }

  void Update() {
    Sprite targetSprite;
    if (violets.isOpen) {
      targetSprite = open;
    } else {
      // countUp goes 0, ..., 7, 8, 9, 10, 11
      var stage0Count = Violets.turnsToChange - flowerStages.Length;
      if (violets.countUp >= stage0Count) {
        targetSprite = flowerStages[violets.countUp - stage0Count];
      } else {
        targetSprite = flowerStages[0];
      }
    }
    if (sr.sprite != targetSprite) {
      sr.sprite = targetSprite;
    }
  }
}