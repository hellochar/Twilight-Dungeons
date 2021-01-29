using System;
using UnityEngine;

[Serializable]
public class ItemShovel : Item {
  public void DigUp(Player player, Grass grass) {
    grass.Kill();
    player.inventory.AddItem(new ItemOfGrass(grass.GetType()));
  }
}

[Serializable]
internal class ItemOfGrass : Item {
  private Type type;

  public ItemOfGrass(Type type) {
    this.type = type;
  }

  public void Plant(Ground ground) {
    var constructor = type.GetConstructor(new Type[1] { typeof(Vector2Int) });
    var newGrass = (Grass) constructor.Invoke(new object[] { ground.pos });
    ground.floor.Put(newGrass);
    Destroy();
  }
}
