using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ParasiteStatusController : StatusController {
  public ParasiteStatus parasite => (ParasiteStatus) status;
  public override void Start() {
    base.Start();
    parasite.OnAttack += HandleAttack;
    HandleAttack();
  }

  private void HandleAttack() {
    StartCoroutine(MoveSprite());
  }

  IEnumerator MoveSprite() {
    var sprite = transform.Find("Sprite");

    var startPos = sprite.localPosition;
    var endPos = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.4f, 0.4f), startPos.z);

    var start = Time.time;
    var dt = 0f;
    do {
      dt = (Time.time - start) / 0.25f;
      var newPos = Vector3.Lerp(startPos, endPos, EasingFunctions.EaseInExpo(0, 1, dt));
      sprite.localPosition = newPos;
      yield return new WaitForEndOfFrame();
    } while (dt < 1);
  }
}
