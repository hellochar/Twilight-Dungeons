using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Item {
  public virtual string displayName =>
  /// get rid of the "Item" prefix
    Util.WithSpaces(GetType().Name.Substring(4));

  public virtual Inventory inventory { get; set; }

  public void Destroy(object unused = null) {
    if (inventory != null) {
      inventory.RemoveItem(this);
    }
  }

  public void Drop(Actor a) {
    if (inventory != null) {
      inventory.DropRandomlyOntoFloorAround(a.floor, a.pos);
    }
  }

  internal virtual string GetStats() => "";

  public virtual List<ActorTask> GetAvailableTasks(Player player) {
    var zeroArgActions = new List<Action<Actor>>();
    zeroArgActions.Add(Destroy);
    zeroArgActions.Add(Drop);
    if (this is IEdible edible) {
      zeroArgActions.Add(edible.Eat);
    }
    if (this is IUsable usable) {
      zeroArgActions.Add(usable.Use);
    }
    return zeroArgActions
      .Select((action) => new GenericTask(player, action))
      .Cast<ActorTask>()
      .ToList();
  }
}

public static class ItemExtensions {
  public static string GetStatsFull(this Item item) {
    var text = item.GetStats();
    if (item is IWeapon w) {
      var (min, max) = w.AttackSpread;
      text += $"\n{min} - {max} damage.";
    }
    if (item is IDurable d) {
      text += $"\nDurability: {d.durability}/{d.maxDurability}.";
    }
    return text.Trim();
  }
}
