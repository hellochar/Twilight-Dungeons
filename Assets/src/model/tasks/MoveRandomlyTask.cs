using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
class MoveRandomlyTask : DoOnceTask {
  public MoveRandomlyTask(Actor actor) : base(actor) { }

  protected override BaseAction GetNextActionImpl() {
    var adjacentTiles = actor.floor.GetAdjacentTiles(actor.pos).Where((tile) => tile.CanBeOccupied());
    if (adjacentTiles.Any()) {
      Vector2Int pos = Util.RandomPick(adjacentTiles).pos;
      return new MoveBaseAction(actor, pos);
    } else {
      return new WaitBaseAction(actor);
    }
  }
}