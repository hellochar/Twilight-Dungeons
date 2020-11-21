using UnityEngine;

public class AttackGroundAction : ActorAction {

  public AttackGroundAction(Actor actor, Vector2Int targetPosition, int turnsTelegraphed = 0) : base(actor) {
    TargetPosition = targetPosition;
    TurnsTelegraphed = turnsTelegraphed;
  }

  public Vector2Int TargetPosition { get; }
  public int TurnsTelegraphed { get; set; }

  public override int Perform() {
    TurnsTelegraphed--;
    if (TurnsTelegraphed >= 0) {
      return actor.baseActionCost;
    }
    if (actor.IsNextTo(TargetPosition)) {
      actor.AttackGround(TargetPosition);
    }
    return base.Perform();
  }

  public override bool IsDone() {
    return TurnsTelegraphed < 0;
  }
}