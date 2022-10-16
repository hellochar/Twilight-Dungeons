using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Plant in your home base, or in the caves to provide a tactical advantage.")]
public class ItemGrass : Item, IConditionallyStackable, ITargetedAction<Ground> {
  public Type grassType;
  public ItemGrass(Type grassType, int stacks) {
    this.grassType = grassType;
    this.stacks = stacks;
  }

  public override string displayName => Util.WithSpaces(grassType.Name);

  public ItemGrass(Type grassType) : this(grassType, 1) { }


  private int _stacks;
  public int stacks {
    get => _stacks;
    set {
      if (value < 0) {
        throw new ArgumentException("Setting negative stack!" + this + " to " + value);
      }
      _stacks = value;
      if (_stacks == 0) {
        Destroy();
      }
    }
  }
  public int stacksMax => 99;

  public string TargettedActionName => "Plant";

  public bool CanStackWith(IConditionallyStackable other) {
    return (other as ItemGrass).grassType == grassType;
  }

  public static bool groundTypeRequirement(Tile t, Type grassType) =>
    // ItemPlaceableEntity.StructureOccupiable(t, grassType);
    ItemPlaceableEntity.HasSpaceForStructure(t, grassType);
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