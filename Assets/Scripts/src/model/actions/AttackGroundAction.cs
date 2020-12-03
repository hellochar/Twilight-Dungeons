using System.Collections.Generic;
using UnityEngine;

/// This is implemented as open-ended but it should be close-ended; but I'm lazy to implement it.
/// Technically open- vs close-ended only makes a difference for the Player, specifically so the UI
/// can respond quicker.
public class AttackGroundAction : ActorAction {

  public AttackGroundAction(Actor actor, Vector2Int targetPosition, int turnsTelegraphed = 0) : base(actor) {
    TargetPosition = targetPosition;
    TurnsTelegraphed = turnsTelegraphed;
  }

  public Vector2Int TargetPosition { get; }
  public int TurnsTelegraphed { get; set; }
  private bool done;

  public override IEnumerator<BaseAction> Enumerator() {
    for (int i = 0; i < TurnsTelegraphed; i++) {
      yield return new WaitBaseAction(actor);
    }
    done = true;
    yield return new AttackGroundBaseAction(actor, TargetPosition);
  }

  public override bool IsDone() => done;
}