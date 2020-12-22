using System;
using System.Linq;
using UnityEngine;

[ObjectInfo("water-pail", "Passed on from your uncle, your trusty water pail is older than you are.")]
public class ItemWaterPail : Item, IStackable {
  public ItemWaterPail() {
    stacks = 0;
  }
  public int stacksMax => 25;
  private int _stacks;
  public int stacks {
    get => _stacks;
    set {
      if (value < 0) {
        throw new ArgumentException("Setting negative stack!" + this + " to " + value);
      }
      _stacks = value;
    }
  }

  public void AddStack() {
    if (stacks >= stacksMax) {
      throw new CannotPerformActionException("Water pail is full!");
    }
    stacks++;
  }

  public void Water(Plant plant) {
    if (stacks > 0) {
      plant.water++;
      stacks--;
    }
  }

  public void Water(Grass grass) {
    if (stacks > 0) {
      var neighborTiles = grass.floor.GetFourNeighbors(grass.pos).Where((tile) => tile is Ground && tile.grass == null);
      var constructorInfo = grass.GetType().GetConstructor(new Type[1] { typeof(Vector2Int) });
      foreach (var tile in neighborTiles) {
        var newGrass = (Grass) constructorInfo.Invoke(new object[] { tile.pos });
        grass.floor.Put(newGrass);
      }
      // grow into a nearby location
      stacks--;
    }
  }
}
