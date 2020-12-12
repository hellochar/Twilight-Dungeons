using System;
using UnityEngine;

public class ItemSeed : Item {
  public Type plantType;

  public ItemSeed(Type plantType) {
    this.plantType = plantType;
  }

  public void Plant(Soil soil) {
    /// consume this item somehow
    var constructorInfo = plantType.GetConstructor(new Type[1] { typeof(Vector2Int) });
    var plant = (Plant) constructorInfo.Invoke(new object[] { soil.pos });
    soil.floor.Add(plant);
    Destroy();
  }

  public override string displayName => $"{Util.WithSpaces(plantType.Name)} Seed";
}
