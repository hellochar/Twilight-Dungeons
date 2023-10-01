using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[Serializable]
public class ItemSeed : Item, IConditionallyStackable, ITargetedAction<Soil> {
  public Type plantType;

  public bool CanStackWith(IConditionallyStackable other) {
    return ((ItemSeed) other).plantType == plantType;
  }

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
  public int stacksMax => 20;
  public int waterCost => plantType.GetCustomAttribute<PlantConfigAttribute>().WaterCost;
  public int floorsToMature => plantType.GetCustomAttribute<PlantConfigAttribute>().FloorsToMature;

  public ItemSeed(Type plantType, int stacks) {
    this.plantType = plantType;
    this.stacks = stacks;
  }

  public ItemSeed(Type plantType) : this(plantType, 1) { }

  public void MoveAndPlant(Soil soil) {
    var model = GameModel.main;
    Player player = model.player;
    if (model.depth != 0) {
      throw new CannotPerformActionException("Plant on the home floor.");
    }
    player.SetTasks(
      new MoveNextToTargetTask(player, soil.pos),
      new GenericPlayerTask(player, () => {
        if (player.IsNextTo(soil)) {
          Plant(soil);
        }
      })
    );
  }

  private void Plant(Soil soil) {
    var player = GameModel.main.player;
    if (player.water >= waterCost) {
      player.water -= waterCost;
      var constructorInfo = plantType.GetConstructor(new Type[1] { typeof(Vector2Int) });
      var plant = (Plant)constructorInfo.Invoke(new object[] { soil.pos });
      soil.floor.Put(plant);
      GameModel.main.stats.plantsPlanted++;
      stacks--;
    } else {
      throw new CannotPerformActionException($"Need <color=lightblue>{waterCost}</color> water!");
    }
  }

  internal override string GetStats() => $"Plant on a Soil to grow into a {Util.WithSpaces(plantType.Name)} Seed.\nCosts <color=lightblue>{waterCost} water</color>.\nMatures in <color=orange>{floorsToMature} floors</color>.";

  public override string displayName => $"{Util.WithSpaces(plantType.Name)} Seed";

  public string TargettedActionName => "Plant";
  public string TargettedActionDescription => $"Choose where to plant {displayName}.";
  public IEnumerable<Soil> Targets(Player player) => player.floor.tiles.Where(tile => tile is Soil && tile.isExplored && tile.CanBeOccupied()).Cast<Soil>();
  public void PerformTargettedAction(Player player, Entity target) => MoveAndPlant((Soil) target);
}
