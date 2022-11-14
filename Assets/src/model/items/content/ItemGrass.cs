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

  public string TargettedActionName => "Plant";
  public string TargettedActionDescription => $"Choose where to plant {displayName}.";
  protected override bool StackingPredicate(Item other) {
    return (other as ItemGrass).grassType == grassType;
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

    var selfAndNeighborsCanBeOccupied = t.floor.GetDiagonalAdjacentTiles(t.pos).All(t => t.CanBeOccupied() || t.body is Player);
    if (!selfAndNeighborsCanBeOccupied) {
      return false;
    }

    if (t.grass != null) {
      return false;
    }

    var neighborGrassesAreSameType = t.floor.GetDiagonalAdjacentTiles(t.pos).All(t => t.grass == null || t.grass.GetType() == grassType);
    if (!neighborGrassesAreSameType) {
      return false;
    }

    return true;
  }

  public void PerformTargettedAction(Player player, Entity target) {
    var floor = target.floor;
    var constructorInfo = grassType.GetConstructor(new Type[1] { typeof(Vector2Int) });
    floor.Put((Entity)constructorInfo.Invoke(new object[] { target.pos }));
    stacks--;
  }

  public IEnumerable<Ground> Targets(Player player) {
    return player.GetVisibleTiles().Where(tile => CanPlantGrassOfType(grassType, tile)).Cast<Ground>();
  }
}