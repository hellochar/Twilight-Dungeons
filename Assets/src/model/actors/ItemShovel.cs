using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[Serializable]
[ObjectInfo("shovel")]
public class ItemShovel : Item, IDurable {
  public ItemShovel() {
    durability = maxDurability;
  }

  public int durability { get; set; }
  public int maxDurability => 7;

  public void DigUp(Player player) {
    var grass = player.grass;
    if (grass != null) {
      var item = new ItemGrass(grass.GetType());
      player.inventory.AddItem(item, grass);
      // they're *not* killed because we don't want to trigger actions on them
      player.floor.Remove(grass);
      this.ReduceDurability();
    }
  }

  public override List<MethodInfo> GetAvailableMethods(Player player) {
    var methods = base.GetAvailableMethods(player);
    methods.Remove(GetType().GetMethod("Destroy"));
    methods.Add(GetType().GetMethod("DigUp"));
    return methods;
  }
}
