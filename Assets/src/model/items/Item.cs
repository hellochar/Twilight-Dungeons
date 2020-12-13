using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Item {
  public virtual string displayName =>
  /// get rid of the "Item" prefix
    Util.WithSpaces(GetType().Name.Substring(4));

  public virtual Inventory inventory { get; set; }

  [PlayerAction]
  public void Destroy() {
    if (inventory != null) {
      inventory.RemoveItem(this);
    }
  }

  internal virtual string GetStats() => "";

  public virtual List<ActorTask> GetAvailableTasks(Player player) {
    var methods = GetType()
      .GetMethods()
      .Where((methodInfo) => methodInfo.GetCustomAttributes(true).OfType<PlayerActionAttribute>().Any()).ToList();

    return methods.Select((methodInfo) => {
      return new GenericTask(player, (p) => methodInfo.Invoke(p, new object[0])).Named(methodInfo.Name);
    }).Cast<ActorTask>().ToList();
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