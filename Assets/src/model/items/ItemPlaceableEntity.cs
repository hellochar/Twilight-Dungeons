using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class ItemPlaceableEntity : Item, ITargetedAction<Ground> {

  public override string displayName => entity.displayName;
  internal override string GetStats() {
    return "Build for one Action Point";
  }

  public bool requiresSpace = false;
  public Entity entity { get; }
  public ItemPlaceableEntity(Entity entity) {
    this.entity = entity;
  }

  internal ItemPlaceableEntity RequireSpace() {
    requiresSpace = true;
    return this;
  }

  string ITargetedAction<Ground>.TargettedActionName => "Build";
  IEnumerable<Ground> ITargetedAction<Ground>.Targets(Player player) {
    var tiles = player.floor.tiles.Where(t => t is Ground && t.isExplored && t.CanBeOccupied()).Cast<Ground>();
    if (requiresSpace) {
      tiles = tiles.Where(tile => tile.floor.GetAdjacentTiles(tile.pos).Where(t => t.CanBeOccupied()).Count() >= 9);
    }
    return tiles;
  }

  void ITargetedAction<Ground>.PerformTargettedAction(Player player, Entity target) {
    if (player.actionPoints < 1) {
      throw new CannotPerformActionException("Need an action point!");
    }
    player.actionPoints--;
    entity.pos = target.pos;
    player.floor.Put(entity);
    Destroy();
  }
}