using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AttackGroundTask : TelegraphedTask {

  public AttackGroundTask(Actor actor, Vector2Int targetPosition, int turns = 0) : base(actor, turns, new AttackGroundBaseAction(actor, targetPosition)) {
    TargetPosition = targetPosition;
  }

  public Vector2Int TargetPosition { get; }
}