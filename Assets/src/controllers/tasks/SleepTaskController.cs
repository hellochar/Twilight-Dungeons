using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SleepTaskController : ActorTaskController {
  private new SleepTask task => (SleepTask) ((ActorTaskController)this).task;

  void Start() {
    if (task.isDeepSleep) {
      GetComponentInChildren<SpriteRenderer>().color = deepSleepColor;
    }
  }

  private static Color deepSleepColor = new Color(0.365f, 0.6712619f, 1, 1);
}
