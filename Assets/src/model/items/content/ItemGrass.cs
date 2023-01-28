using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Plant in your home base, or in the caves to provide a tactical advantage.")]
public class ItemGrass : Item, ITargetedAction<Ground> {
  public Type grassType;
  public ItemGrass(Type grassType, int stacks) : base(stacks) {
    this.grassType = grassType;
  }
  public ItemGrass(Type grassType) : this(grassType, 1) { }


  public override string displayName => Util.WithSpaces(grassType.Name);

  public override int stacksMax => 99;

  public string TargettedActionName => "Plant (10 water)";
  public string TargettedActionDescription => $"Choose where to plant {displayName}.";
  protected override bool StackingPredicate(Item other) {
    return (other as ItemGrass).grassType == grassType;
  }

  internal override string GetStats() {
    var constructorInfo = grassType.GetConstructor(new Type[1] { typeof(Vector2Int) });
    Grass grass = (Grass)constructorInfo.Invoke(new object[] { Vector2Int.zero });
    return "Plant at home or in the caves.\n" + grass.description;
  }

  // we can plant a Grass of type Type at tile T if:
  // T can be occupied
  // T doesn't already have a Grass
  // all of T's neighbors can be occupied
  // all of T's neighbors either have no grass, or has the same type Grass.
  public static bool CanPlantGrassOfType(Type grassType, Tile t) {
    if (!(t is Ground)) {
      return false;
    }

    var home = t.floor as HomeFloor;
    if (home == null) {
      return false;
    }

    // if (home.soils[t.pos] == null) {
    //   return false;
    // }

    if (home.pieces[t.pos] != null) {
      return false;
    }

    if (t.grass != null) {
      return false;
    }

    if (t.body != null) {
      return false;
    }

    // var selfAndNeighborsCanBeOccupied = t.floor.GetDiagonalAdjacentTiles(t.pos).All(t => t.CanBeOccupied() || t.body is Player);
    // if (!selfAndNeighborsCanBeOccupied) {
    //   return false;
    // }

    // var neighborGrassesAreSameType = t.floor.GetDiagonalAdjacentTiles(t.pos).All(t => t.grass == null || t.grass.GetType() == grassType);
    // if (!neighborGrassesAreSameType) {
    //   return false;
    // }

    return true;
  }

  public void PerformTargettedAction(Player player, Entity target) {
    // if (!(player.floor is HomeFloor)) {
    //   throw new CannotPerformActionException("Plant at home!");
    // }
    player.SetTasks(
      new MoveNextToTargetTask(player, target.pos),
      new GenericOneArgTask<Vector2Int>(player, PlaceGrass, target.pos)
    );
  }

  private void PlaceGrass(Vector2Int pos) {
    var player = GameModel.main.player;
    player.UseResourcesOrThrow(10, 0, 0);
    if (player.pos == pos) {
      // move them off the target
      player.floor.BodyPlacementBehavior(player);
    }
    var floor = player.floor;
    var constructorInfo = grassType.GetConstructor(new Type[1] { typeof(Vector2Int) });
    floor.Put((Entity)constructorInfo.Invoke(new object[] { pos }));
    stacks--;
  }

  public IEnumerable<Ground> Targets(Player player) {
    return player.GetVisibleTiles().Where(tile => CanPlantGrassOfType(grassType, tile)).Cast<Ground>();
  }
}