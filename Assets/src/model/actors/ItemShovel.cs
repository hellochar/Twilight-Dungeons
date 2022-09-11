using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[Serializable]
[ObjectInfo("shovel")]
public class ItemShovel : Item {
  public void DigUp(Player player) {
    if (player.grass != null) {
      player.grass.Uproot();
    }
  }

  public override List<MethodInfo> GetAvailableMethods(Player player) {
    var methods = base.GetAvailableMethods(player);
    methods.Remove(GetType().GetMethod("Destroy"));
    methods.Add(GetType().GetMethod("DigUp"));
    return methods;
  }
}
