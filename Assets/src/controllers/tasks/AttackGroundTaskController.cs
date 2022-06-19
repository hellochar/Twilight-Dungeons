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
    var target = Util.withZ(task.TargetPosition);

    lr.SetPosition(0, start);

    Bounds targetBounds = new Bounds(target, new Vector3(1, 1, 1));
    Ray offset = new Ray(start, target - start);
    targetBounds.IntersectRay(offset, out float T);
    var boundedPosition = offset.GetPoint(T);
    var finalPosition = Vector3.Lerp(boundedPosition, target, 0.2f);
    lr.SetPosition(1, finalPosition);
    Update();
  }

  public override void Update() {
    base.Update();
    Transform reticle = transform.Find("Reticle");
    reticle.position = Util.withZ(TargetPosition, reticle.position.z);
  }
}
