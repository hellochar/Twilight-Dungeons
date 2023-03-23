using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Can be eaten or Processed.")]
public class ItemGrass : Item,
// ITargetedAction<Ground>,
IEdible {
  public Type grassType;
  public ItemGrass(Type grassType, int stacks) : base(stacks) {
    this.grassType = grassType;
  }
  public ItemGrass(Type grassType) : this(grassType, 1) { }


  public override string displayName => Util.WithSpaces(grassType.Name);

  public override int stacksMax => 1;

  protected override bool StackingPredicate(Item other) {
    return (other as ItemGrass).grassType == grassType;
  }

  public virtual void Eat(Actor a) {
    if (a is Player p) {
      var eatMethod = grassType.GetMethod("Eat");
      if (eatMethod != null) {
        eatMethod.Invoke(null, new object[] { p });
      } else {
        Grass.Eat(p);
      }
      stacks--;
    }
  }

  internal override string GetStats() {
    // var constructorInfo = grassType.GetConstructor(new Type[1] { typeof(Vector2Int) });
    // Grass grass = (Grass)constructorInfo.Invoke(new object[] { Vector2Int.zero });
    // return "Plant at home or in the caves.\n" + grass.description;
    return "Eat, or use it in various structures.";
  }

  // we can plant a Grass of type Type at tile T if:
  // T can be occupied
  // T doesn't already have a Grass
  // all of T's neighbors can be occupied
  // all of T's neighbors either have no grass, or has the same type Grass.
  // public static bool CanPlantGrassOfType(Type grassType, Tile t) {
  //   if (!(t is Ground)) {
  //     return false;
  //   }

  //   // var home = t.floor as HomeFloor;
  //   // if (home == null) {
  //   //   return false;
  //   // }

  //   // // if (home.soils[t.pos] == null) {
  //   // //   return false;
  //   // // }

  //   // if (home.pieces[t.pos] != null) {
  //   //   return false;
  //   // }

  //   if (t.grass != null) {
  //     return false;
  //   }

  //   if (t.body != null) {
  //     return false;
  //   }

  //   // var selfAndNeighborsCanBeOccupied = t.floor.GetDiagonalAdjacentTiles(t.pos).All(t => t.CanBeOccupied() || t.body is Player);
  //   // if (!selfAndNeighborsCanBeOccupied) {
  //   //   return false;
  //   // }

  //   // var neighborGrassesAreSameType = t.floor.GetDiagonalAdjacentTiles(t.pos).All(t => t.grass == null || t.grass.GetType() == grassType);
  //   // if (!neighborGrassesAreSameType) {
  //   //   return false;
  //   // }

  //   return true;
    // }

  // public string TargettedActionName => "Plant";
  // public string TargettedActionDescription => $"Choose where to plant {displayName}.";
  //   public void PerformTargettedAction(Player player, Entity target) {
  // #if !experimental_cavenetwork
  //     if (!(player.floor is HomeFloor)) {
  //       throw new CannotPerformActionException("Plant at home!");
  //     }
  // #endif
  //     // player.UseResourcesOrThrow(20, 0, 0);
  //     player.SetTasks(
  //       new MoveNextToTargetTask(player, target.pos),
  //       new GenericOneArgTask<Vector2Int>(player, PlaceGrass, target.pos)
  //     );
  //   }

  public void PlaceGrass(Vector2Int pos) {
    var player = GameModel.main.player;
    if (player.pos == pos) {
      // move them off the target
      player.floor.BodyPlacementBehavior(player);
    }
    var floor = player.floor;
    var constructorInfo = grassType.GetConstructor(new Type[1] { typeof(Vector2Int) });
    floor.Put((Entity)constructorInfo.Invoke(new object[] { pos }));
    stacks--;
  }

  // public IEnumerable<Ground> Targets(Player player) {
  //   return player.GetVisibleTiles().Where(tile => CanPlantGrassOfType(grassType, tile)).Cast<Ground>();
  // }
}