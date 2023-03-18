using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class ItemPlaceableEntity : Item, ITargetedAction<Ground> {

  public override string displayName => entity.displayName;
  internal override string GetStats() => $"{ ObjectInfo.GetDescriptionFor(entity) }\n\nBuild for one Action Point.";

  public Entity entity { get; }
  public ItemPlaceableEntity(Entity entity) {
    this.entity = entity;
  }

  // We can place the Body at tile T if:
  // T can be occupied
  // all of T's neighbors either have no Body, or has the same type Body
  public static bool CanPlaceEntityOfType(Type entityType, Tile t) {
    if (!(t is Ground)) {
      return false;
    }

    if (!t.CanBeOccupied()) {
      return false;
    }

    if (t.grass != null) {
      return false;
    }

#if !experimental_cavenetwork
    var home = t.floor as HomeFloor;
    if (home == null) {
      return false;
    }
#endif

    // var isStation = entityType.IsSubclassOf(typeof(Station));
    // if (!isStation) {
    //   // only place on soils
    //   if (t.soil == null) {
    //     return false;
    //   }
    // }

    // var selfAndNeighborsCanBeOccupied = t.floor.GetDiagonalAdjacentTiles(t.pos).All(t => {
    //   if (t.body is Player) {
    //     return true;
    //   }

    //   if (t.body == null) {
    //     return true;
    //   }

    //   if (t.body.GetType() == entityType) {
    //     return true;
    //   }

    //   return false;
    // });

    // if (!selfAndNeighborsCanBeOccupied) {
    //   return false;
    // }

    return true;
  }

  string ITargetedAction<Ground>.TargettedActionName => "Place";
  string ITargetedAction<Ground>.TargettedActionDescription => $"Choose where to place the {entity.displayName}.";
  IEnumerable<Ground> ITargetedAction<Ground>.Targets(Player player) {
    var entityType = entity is GrowingEntity g ? g.inner.GetType() : entity.GetType();
    return player.GetVisibleTiles().Where(tile => CanPlaceEntityOfType(entityType, tile)).Cast<Ground>();
  }

  public static bool StructureOccupiable(Tile t, Type type = null) {
     return t is Ground && (t.CanBeOccupied() || t.body is Player) && (t.grass == null || t.grass.GetType() == type);
  }

  void ITargetedAction<Ground>.PerformTargettedAction(Player player, Entity target) {
    if (!(player.floor is HomeFloor)) {
      throw new CannotPerformActionException("Place at home!");
    }
    // if it's growing, it's free
    // if (entity is GrowingEntity) {
    // } else {
    //   player.UseActionPointOrThrow();
    // }
    player.SetTasks(
      new MoveNextToTargetTask(player, target.pos),
      new GenericOneArgTask<Vector2Int>(player, PlaceEntity, target.pos)
    );
  }

  public void PlaceEntity(Vector2Int pos) {
    var player = GameModel.main.player;
    if (player.pos == pos) {
      // move them off the target
      player.floor.BodyPlacementBehavior(player);
    }
    entity.pos = pos;
    player.floor.Put(entity);
    Destroy();
  }
}