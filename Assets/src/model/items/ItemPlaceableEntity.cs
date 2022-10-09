using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class ItemPlaceableEntity : Item, ITargetedAction<Ground> {

  public override string displayName => entity.displayName;
  internal override string GetStats() => $"{ ObjectInfo.GetDescriptionFor(entity) }\n\nBuild for one Action Point.";

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
    var entityType = entity is GrowingEntity g ? g.inner.GetType() : entity.GetType();
    var tiles = player.floor.tiles.Where(t => t is Ground && t.isExplored && StructureOccupiable(t, entityType)).Cast<Ground>();
    if (requiresSpace) {
      tiles = tiles.Where(tile => tile.floor.GetAdjacentTiles(tile.pos).Where(t => StructureOccupiable(t, entityType)).Count() >= 9);
    }
    return tiles;
  }

  public static bool StructureOccupiable(Tile t, Type placingType) {
    return t.CanBeOccupied() || t.body is Player || (t.body != null && t.body.GetType() == placingType);
  }

  void ITargetedAction<Ground>.PerformTargettedAction(Player player, Entity target) {
    // if it's growing, it's free
    if (entity is GrowingEntity) {
    } else {
      player.UseActionPointOrThrow();
    }
    if (player.pos == target.pos) {
      // move them off the target
      player.floor.BodyPlacementBehavior(player);
    }
    entity.pos = target.pos;
    player.floor.Put(entity);
    Destroy();
  }
}