using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Item {
  public virtual string displayName =>
  /// get rid of the "Item" prefix
    Util.WithSpaces(GetType().Name.Substring(4));

  public virtual Inventory inventory { get; set; }
  /// remove this item from the inventory
  public void Destroy(Actor a) {
    if (inventory != null) {
      inventory.RemoveItem(this);
    }
  }

  internal virtual string GetStats() => "";

  public virtual List<ActorTask> GetAvailableTasks(Player player) {
    return new List<ActorTask> {
      new GenericTask(player, Destroy)
    };
  }
}
