using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class ItemPlaceableEntity : Item, ITargetedAction<Ground> {

  public override string displayName => entity.displayName;
  internal override string GetStats() => $"{ ObjectInfo.GetDescriptionFor(entity) }\n\nBuild for one Action Point.";

  public bool requiresSpace = true;
  public Entity entity { get; }
  public ItemPlaceableEntity(Entity entity) {
    this.entity = entity;
  }

  internal ItemPlaceableEntity RequireSpace() {
    requiresSpace = true;
    return this;
  }

  string ITargetedAction<Ground>.TargettedActionName => "Place";
  string ITargetedAction<Ground>.TargettedActionDescription => $"Choose where to place the {entity.displayName}.";
  IEnumerable<Ground> ITargetedAction<Ground>.Targets(Player player) {
    var entityType = entity is GrowingEntity g ? g.inner.GetType() : entity.GetType();
    var tiles = player.floor.tiles.Where(t => t is Ground && t.isExplored && StructureOccupiable(t)).Cast<Ground>();
    if (requiresSpace) {
      tiles = tiles.Where(t => HasSpaceForStructure(t));
    }
    return tiles;
  }

  public static bool StructureOccupiable(Tile t, Type grassType = null) {
     return t is Ground && (t.CanBeOccupied() || t.body is Player) && (t.grass == null || t.grass.GetType() == grassType);
  }

  public static bool HasSpaceForStructure(Tile tile, Type grassType = null) {
    return (tile.floor.GetDiagonalAdjacentTiles(tile.pos).Where(t => StructureOccupiable(t, grassType)).Count() >= 9);
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