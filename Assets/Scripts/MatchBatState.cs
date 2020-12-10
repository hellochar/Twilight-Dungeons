using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MatchBatState : ActorController {

  public override void Update() {
    base.Update();
    if (actor.task is WaitTask || actor.task is SleepTask) {
      transform.localScale = Vector3.Lerp(transform.localScale, scaleWaiting, 10f * Time.deltaTime);
    } else {
      transform.localScale = Vector3.Lerp(transform.localScale, scaleNormal, 10f * Time.deltaTime);
    }
  }

  public static Vector3 scaleNormal = new Vector3(1, 1, 1);
  public static Vector3 scaleWaiting = new Vector3(1, -1, 1);
}
