using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchGrappledTask : MatchActorTask<GrappledTask> {

  public override void Start() {
    transform.localPosition = new Vector3(0, 0, 0);
    base.Start();
  }
}
