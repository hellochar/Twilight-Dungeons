using System.Linq;
using UnityEngine;

class MoveRandomlyAction : ActorAction {
  public MoveRandomlyAction(Actor actor) : base(actor) { }

  public override void Perform() {
    var adjacentTiles = actor.floor.GetAdjacentTiles(actor.pos).Where((tile) => tile.CanBeOccupied());
    Vector2Int pos = Util.RandomPick(adjacentTiles).pos;
    actor.pos = pos;
    base.Perform();
  }
}