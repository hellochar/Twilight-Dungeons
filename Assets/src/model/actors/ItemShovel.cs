using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[Serializable]
[ObjectInfo("shovel")]
public class ItemShovel : Item {
  public ItemShovel(int stacks) : base(stacks) {}

  public override int stacksMax => int.MaxValue;
  // public override bool disjoint => true;

  public void DigUp(Player player) {
    var grass = player.grass;
    if (grass != null) {
      var item = new ItemGrass(grass.GetType());
      player.inventory.AddItem(item, grass);
      // they're *not* killed because we don't want to trigger actions on them
      player.floor.Remove(grass);
      stacks--;
    }
  }

  public override List<MethodInfo> GetAvailableMethods(Player player) {
    var methods = base.GetAvailableMethods(player);
    methods.Remove(GetType().GetMethod("Destroy"));
    methods.Add(GetType().GetMethod("DigUp"));
    return methods;
  }
}
