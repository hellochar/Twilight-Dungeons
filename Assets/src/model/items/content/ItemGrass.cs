using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Plant in your home base to stop Soil Hardening, or in the caves to provide a tactical advantage.")]
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

  public void PerformTargettedAction(Player player, Entity target) {
    var floor = target.floor;
    var toPlant = floor.BreadthFirstSearch(target.pos, t => t is Ground && t.CanBeOccupied() && t.grass == null).Take(stacks).ToList();
    var constructorInfo = grassType.GetConstructor(new Type[1] { typeof(Vector2Int) });
    floor.PutAll(toPlant.Select(t => {
      return (Grass)constructorInfo.Invoke(new object[] { t.pos });
    }));
    stacks -= toPlant.Count;
  }

  public IEnumerable<Ground> Targets(Player player) {
    return player.floor.tiles.Where(tile => tile is Ground && tile.CanBeOccupied() && tile.grass == null).Cast<Ground>();
  }
}