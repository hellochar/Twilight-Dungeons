using System;
using UnityEngine;

[Serializable]
public class ItemSeed : Item {
  public Type plantType;

  public ItemSeed(Type plantType) {
    this.plantType = plantType;
  }

  public void Plant(Soil soil) {
    if (GameModel.main.player.water >= 100) {
      GameModel.main.player.water -= 100;
      var constructorInfo = plantType.GetConstructor(new Type[1] { typeof(Vector2Int) });
      var plant = (Plant) constructorInfo.Invoke(new object[] { soil.pos });
      soil.floor.Put(plant);
      Destroy();
    } else {
      throw new CannotPerformActionException("Need 100 water!");
    }
  }

  internal override string GetStats() => $"Plant on a Soil (requires 100 water).\nMatures in 320 turns.";

  public override string displayName => $"{Util.WithSpaces(plantType.Name)} Seed";
}
