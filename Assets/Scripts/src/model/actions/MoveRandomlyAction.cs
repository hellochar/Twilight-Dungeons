using UnityEngine;

class MoveRandomlyAction : ActorAction {
  public MoveRandomlyAction(Actor actor) : base(actor) { }

  public override int Perform() {
    Vector2Int dir = (new Vector2Int[] {
      Vector2Int.up,
      Vector2Int.down,
      Vector2Int.left,
      Vector2Int.right,
    })[UnityEngine.Random.Range(0, 4)];
    actor.pos += dir;
    return base.Perform();
  }
}