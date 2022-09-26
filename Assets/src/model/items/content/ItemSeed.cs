using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class ItemSeed : Item, IConditionallyStackable, ITargetedAction<Ground> {
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
  public int waterCost;

  public ItemSeed(Type plantType, int stacks) {
    this.plantType = plantType;
    this.stacks = stacks;
    this.waterCost = (int?) plantType.GetProperty("waterCost")?.GetValue(null) ?? 100;
  }

  public ItemSeed(Type plantType) : this(plantType, 1) { }

  public void MoveAndPlant(Ground soil) {
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

  private void Plant(Ground soil) {
    var player = GameModel.main.player;
    if (player.water < waterCost) {
      throw new CannotPerformActionException($"Need <color=lightblue>{waterCost}</color> water!");
    }
#if experimental_actionpoints
    if (player.actionPoints < 1) {
      throw new CannotPerformActionException("Need an action point!");
    }
    player.actionPoints--;
#endif
    player.water -= waterCost;
    var constructorInfo = plantType.GetConstructor(new Type[1] { typeof(Vector2Int) });
    var plant = (Plant)constructorInfo.Invoke(new object[] { soil.pos });
    var floor = soil.floor;
    floor.Put(plant);

#if experimental_grasscovering
    var adjacentTiles = floor.GetAdjacentTiles(soil.pos).ToList();
    foreach (var tile in adjacentTiles) {
      if (tile.grass != null) {
        tile.grass.Kill(plant);
      } else if (tile is Ground) {
        if (tile is Soil) {
          floor.Put(new Ground(tile.pos));
        } else {
          floor.Put(new HardGround(tile.pos));
        }
      }
    }
#endif
    GameModel.main.stats.plantsPlanted++;
    stacks--;
  }

  internal override string GetStats() => $"Plant on a Soil - costs <color=lightblue>{waterCost}</color> water.\nMatures in 320 turns.";

  public override string displayName => $"{Util.WithSpaces(plantType.Name)} Seed";

  public string TargettedActionName => "Plant";
  public IEnumerable<Ground> Targets(Player player) =>
#if experimental_grasscovering
      player.floor.tiles.Where(tile =>
        tile is Ground && tile.isExplored && tile.CanBeOccupied()
        && tile.floor.GetAdjacentTiles(tile.pos).Where(t => t is Ground).Count() >= 9
      ).Cast<Ground>();
#else
      (player.floor.tiles.Where(tile => tile is Soil && tile.isExplored && tile.CanBeOccupied()).Cast<Ground>());
#endif

  public void PerformTargettedAction(Player player, Entity target) => MoveAndPlant((Ground) target);
}
