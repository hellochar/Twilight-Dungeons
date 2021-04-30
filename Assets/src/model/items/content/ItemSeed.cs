using System;
using UnityEngine;

[Serializable]
public class ItemSeed : Item, IConditionallyStackable {
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
    if (player.water >= 100) {
      player.water -= 100;
      var constructorInfo = plantType.GetConstructor(new Type[1] { typeof(Vector2Int) });
      var plant = (Plant)constructorInfo.Invoke(new object[] { soil.pos });
      soil.floor.Put(plant);
      stacks--;
    }
  }

  internal override string GetStats() => $"Plant on a Soil (requires 100 water).\nMatures in 320 turns.";

  public override string displayName => $"{Util.WithSpaces(plantType.Name)} Seed";
}
