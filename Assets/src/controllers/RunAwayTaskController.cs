using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunAwayTaskController : ActorTaskController<RunAwayTask> {

  public override void Start() {
    transform.localPosition = new Vector3(0, 0.5f, 0);
    base.Start();
  }
}
