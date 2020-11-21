using UnityEngine;

public class AttackGroundAction : ActorAction {

  public AttackGroundAction(Actor actor, Vector2Int targetPosition, int turnsTelegraphed = 0) : base(actor) {
    TargetPosition = targetPosition;
    TurnsTelegraphed = turnsTelegraphed;
  }

  public Vector2Int TargetPosition { get; }
  public int TurnsTelegraphed { get; set; }

  public override void Perform() {
    TurnsTelegraphed--;
    if (TurnsTelegraphed >= 0) {
      return;
    }
    if (actor.IsNextTo(TargetPosition)) {
      actor.AttackGround(TargetPosition);
    }
  }

  public override bool IsDone() {
    return TurnsTelegraphed < 0;
  }
}