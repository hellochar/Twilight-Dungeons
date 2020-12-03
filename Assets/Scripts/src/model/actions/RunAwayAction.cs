using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RunAwayAction : ActorAction {
  private Vector2Int fearPoint;
  public int turns;
  public int turnsRemaining;

  public RunAwayAction(Actor a, Vector2Int fearPoint, int turns) : base(a) {
    this.fearPoint = fearPoint;
    this.turns = turns;
    this.turnsRemaining = turns;
  }

  public override IEnumerator<BaseAction> Enumerator() {
    yield return new WaitBaseAction(actor);
    for (; turnsRemaining > 0; turnsRemaining--) {
      var adjacentTiles = actor.floor.GetAdjacentTiles(actor.pos).Where((tile) => tile.CanBeOccupied());
      if (adjacentTiles.Any()) {
        var furthestTile = adjacentTiles.Aggregate((t1, t2) =>
          Vector2Int.Distance(fearPoint, t1.pos) > Vector2Int.Distance(fearPoint, t2.pos) ? t1 : t2);
        yield return new MoveBaseAction(actor, furthestTile.pos);
      } else {
        yield return new WaitBaseAction(actor);
      }
    }
  }
}
