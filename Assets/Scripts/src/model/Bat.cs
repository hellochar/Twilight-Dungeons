using UnityEngine;

public class Bat : Actor {
  class BatAIAction : ActorAction {
    internal BatAIAction(Bat bat) : base(bat) {}

    public override int Perform() {
      // randomly move 
      Vector2Int dir = (new Vector2Int[] {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right,
      })[UnityEngine.Random.Range(0, 4)];
      actor.pos += dir;
      return actor.baseActionCost;
    }

    public override bool IsDone() {
      return false;
    }
  }

  public Bat(Vector2Int pos) : base(pos) {
    this.action = new BatAIAction(this);
  }
}
