using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchAttackGroundAction : MonoBehaviour {
  public Actor actor;
  public AttackGroundAction action;

  void Start() {
    actor = GetComponentInParent<MatchActorState>().actor;
    action = (AttackGroundAction) actor.action;

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

  void Update() {
    if (actor.action != action) {
      Destroy(this.gameObject);
      return;
    }
    Transform reticle = transform.Find("Reticle");
    reticle.position = Util.withZ(action.TargetPosition, reticle.position.z);
  }
}
