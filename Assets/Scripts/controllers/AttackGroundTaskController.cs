using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackGroundTaskController : ActorTaskController<AttackGroundTask> {

  public override void Start() {
    base.Start();
    var straight = transform.Find("Connector Straight").gameObject;
    var diagonal = transform.Find("Connector Diagonal").gameObject;

    var offset = action.TargetPosition - actor.pos;
    var isDiagonal = !(offset.x == 0 || offset.y == 0);
    if (isDiagonal) {
      Destroy(straight);
      var rotZ = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
      diagonal.transform.rotation = Quaternion.Euler(0, 0, rotZ + 45);
    } else {
      Destroy(diagonal);
      var rotZ = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
      straight.transform.rotation = Quaternion.Euler(0, 0, rotZ);
    }
    Update();
  }

  public override void Update() {
    base.Update();
    Transform reticle = transform.Find("Reticle");
    reticle.position = Util.withZ(action.TargetPosition, reticle.position.z);
  }
}
