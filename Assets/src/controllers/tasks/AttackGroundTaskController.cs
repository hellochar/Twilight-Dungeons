using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackGroundTaskController : ActorTaskController {
  private new AttackGroundTask task => (AttackGroundTask) ((ActorTaskController)this).task;
  public LineRenderer lr;

  public virtual Vector2Int TargetPosition => task.TargetPosition;
  public Vector2Int offset => TargetPosition - actor.pos;

  public void Start() {
    var start = Util.withZ(task.actor.pos);
    var target = Util.withZ(TargetPosition);

    lr.SetPosition(0, start);

    Bounds targetBounds = new Bounds(target, new Vector3(1, 1, 1));
    Ray offsetRay = new Ray(start, target - start);
    targetBounds.IntersectRay(offsetRay, out float T);
    var boundedPosition = offsetRay.GetPoint(T);
    var finalPosition = Vector3.Lerp(boundedPosition, target, 0.2f);
    lr.SetPosition(1, finalPosition);
    
    if (Util.DiamondMagnitude(offset) > 1) {
      lr.widthMultiplier = 0.03f;
    }

    Transform reticle = transform.Find("Reticle");
    reticle.position = Util.withZ(TargetPosition, reticle.position.z);
  }
}
