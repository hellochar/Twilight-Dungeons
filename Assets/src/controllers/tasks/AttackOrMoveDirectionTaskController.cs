using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackOrMoveDirectionTaskController : AttackGroundTaskController {
  protected override bool removeImmediately => true;
  private new AttackOrMoveDirectionTask task => (AttackOrMoveDirectionTask) ((ActorTaskController)this).task;
  public override Vector2Int TargetPosition => task.actor.pos + task.Offset;
}
