using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class RunAwayTask : ActorTask {
  public override TaskStage WhenToCheckIsDone => TaskStage.After;
  private Vector2Int fearPoint;
  public int turns;
  public int turnsRemaining;
  public bool hasSurpriseTurn;

  public RunAwayTask(Actor a, Vector2Int fearPoint, int turns, bool hasSurpriseTurn = true) : base(a) {
    this.fearPoint = fearPoint;
    this.turns = turns;
    this.turnsRemaining = turns;
    this.hasSurpriseTurn = hasSurpriseTurn;
  }

  protected override BaseAction GetNextActionImpl() {
    if (hasSurpriseTurn) {
      hasSurpriseTurn = false;
      return new WaitBaseAction(actor);
    }
    turnsRemaining--;
    var adjacentTiles = actor.floor.GetAdjacentTiles(actor.pos).Where((tile) => tile.CanBeOccupied());
    if (adjacentTiles.Any()) {
      var furthestTile = adjacentTiles.Aggregate((t1, t2) =>
        Vector2Int.Distance(fearPoint, t1.pos) > Vector2Int.Distance(fearPoint, t2.pos) ? t1 : t2);
      return new MoveBaseAction(actor, furthestTile.pos);
    } else {
      return new WaitBaseAction(actor);
    }
  }

  public override bool IsDone() => turnsRemaining <= 0;
}
