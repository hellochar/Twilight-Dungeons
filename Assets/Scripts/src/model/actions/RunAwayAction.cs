using System.Linq;
using UnityEngine;

public class RunAwayAction : ActorAction {
  private Vector2Int fearPoint;
  public int turns;

  public RunAwayAction(Actor a, Vector2Int fearPoint, int turns) : base(a) {
    this.fearPoint = fearPoint;
    this.turns = turns;
  }

  public override void Perform() {
    turns--;
    var adjacentTiles = actor.floor.GetNeighborhoodTiles(actor.pos).Where((tile) => tile.CanBeOccupied());
    var furthestTile = adjacentTiles.Aggregate((t1, t2) =>
      Vector2Int.Distance(fearPoint, t1.pos) > Vector2Int.Distance(fearPoint, t2.pos) ? t1 : t2);
    actor.pos = furthestTile.pos;
  }

  public override bool IsDone() {
    return turns <= 0;
  }
}
