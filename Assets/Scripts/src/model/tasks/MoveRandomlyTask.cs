using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class MoveRandomlyTask : DoOnceTask {
  public MoveRandomlyTask(Actor actor) : base(actor) { }

  public override IEnumerator<BaseAction> Enumerator() {
    var adjacentTiles = actor.floor.GetAdjacentTiles(actor.pos).Where((tile) => tile.CanBeOccupied());
    if (adjacentTiles.Any()) {
      Vector2Int pos = Util.RandomPick(adjacentTiles).pos;
      yield return new MoveBaseAction(actor, pos);
    }
  }
}