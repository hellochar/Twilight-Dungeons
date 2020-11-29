using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MatchBatState : MatchActorState {

  public override void Update() {
    base.Update();
    if (actor.action is WaitAction || actor.action is SleepAction) {
      transform.localScale = Vector3.Lerp(transform.localScale, scaleWaiting, 10f * Time.deltaTime);
    } else {
      transform.localScale = Vector3.Lerp(transform.localScale, scaleNormal, 10f * Time.deltaTime);
    }
  }

  public static Vector3 scaleNormal = new Vector3(1, 1, 1);
  public static Vector3 scaleWaiting = new Vector3(1, -1, 1);
}
