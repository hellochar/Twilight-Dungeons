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
  public int maxDurability => 10;

  public void DigUp(Player player) {
    var grass = player.grass;
    if (grass != null) {
    // player.UseActionPointOrThrow();
    // if (floor.EnemiesLeft() == 0 && floor.availableToPickGrass) {
      // floor.availableToPickGrass = false;
      // var whichGrasses = floor.BreadthFirstSearch(pos, t => t.grass?.GetType() == GetType()).Select(t => t.grass).ToList();
      var whichGrasses = new Grass[] { grass };
      var item = new ItemGrass(grass.GetType(), whichGrasses.Length);
      player.inventory.AddItem(item, grass);
      // they're *not* killed because we don't want to trigger actions on them
      player.floor.RemoveAll(whichGrasses);
      this.ReduceDurability();
    // }
    }
  }

  public override List<MethodInfo> GetAvailableMethods(Player player) {
    var methods = base.GetAvailableMethods(player);
    methods.Remove(GetType().GetMethod("Destroy"));
    methods.Add(GetType().GetMethod("DigUp"));
    return methods;
  }
}
