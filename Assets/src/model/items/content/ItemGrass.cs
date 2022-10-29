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

  protected override bool StackingPredicate(Item other) {
    return (other as ItemGrass).grassType == grassType;
  }

  public static bool groundTypeRequirement(Tile t, Type grassType) =>
    // ItemPlaceableEntity.StructureOccupiable(t, grassType);
    // ItemPlaceableEntity.HasSpaceForStructure(t, grassType);
    t is Ground && t.CanBeOccupied();
    // t is Ground && (t.floor.GetAdjacentTiles(t.pos).Where(t => t.CanBeOccupied()).Count() >= 9);
    //  grassType == typeof(SoftMoss) ?
    //  (t is Ground) :
    //  (t is Soil);

  public void PerformTargettedAction(Player player, Entity target) {
    var floor = target.floor;
    var constructorInfo = grassType.GetConstructor(new Type[1] { typeof(Vector2Int) });
    floor.Put((Entity)constructorInfo.Invoke(new object[] { target.pos }));
    stacks--;
  }

  public IEnumerable<Ground> Targets(Player player) {
    return player.GetVisibleTiles().Where(tile => groundTypeRequirement(tile, grassType)).Cast<Ground>();
  }
}