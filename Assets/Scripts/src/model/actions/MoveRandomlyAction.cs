using UnityEngine;

class MoveRandomlyAction : ActorAction {
  public MoveRandomlyAction(Actor actor) : base(actor) { }

  public override void Perform() {
    Vector2Int dir = Util.RandomAdjacentDirection();
    actor.pos += dir;
    base.Perform();
  }
}