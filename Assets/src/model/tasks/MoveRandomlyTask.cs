using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
class MoveRandomlyTask : DoOnceTask {
  private Func<Tile, bool>? predicate;

  public MoveRandomlyTask(Actor actor, Func<Tile, bool>? predicate = null) : base(actor) {
    this.predicate = predicate;
  }

  protected override BaseAction GetNextActionImpl() {
    return GetRandomMove(actor, predicate);
  }

  public static BaseAction GetRandomMove(Actor actor, Func<Tile, bool>? predicate = null) {
    var adjacentTiles = actor.floor?.GetAdjacentTiles(actor.pos).Where((tile) => tile.CanBeOccupied());
    if (predicate != null) {
      adjacentTiles = adjacentTiles.Where(predicate);
    }
    if (adjacentTiles?.Any() ?? false) {
      Vector2Int pos = Util.RandomPick(adjacentTiles).pos;
      return new MoveBaseAction(actor, pos);
    } else {
      return new WaitBaseAction(actor);
    }
  }
}