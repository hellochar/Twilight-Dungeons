using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[Serializable]
public class Item : IModifierProvider {
  public virtual string displayName =>
  /// get rid of the "Item" prefix
    Util.WithSpaces(GetType().Name.Substring(4));

  public virtual Inventory inventory { get; set; }

  [field:NonSerialized] /// controller only
  public event Action OnDestroyed;
  public List<IItemMod> mods = new List<IItemMod>();
  public virtual IEnumerable<object> MyModifiers => mods;

  // must take a parameter so ItemController can invoke this without erroring
  public void Destroy(object unused = null) {
    if (inventory != null) {
      OnDestroyed?.Invoke();
      inventory.OnItemDestroyed?.Invoke(this);
      inventory.RemoveItem(this);
    }
  }

  public void Drop(Actor a) {
    if (inventory != null) {
      inventory.TryDropItem(a.floor, a.pos, this);
    }
  }

  internal virtual string GetStats() {
    return ObjectInfo.GetDescriptionFor(this);
  }

  public virtual List<MethodInfo> GetAvailableMethods(Player player) {
    var methods = new List<MethodInfo>() {
      // GetType().GetMethod("Destroy"),
      GetType().GetMethod("Drop")
    };
    if (this is IEdible edible) {
      methods.Add(GetType().GetMethod("Eat"));
    }
    if (this is IUsable usable) {
      methods.Add(GetType().GetMethod("Use"));
    }
    return methods;
  }
}

public interface IItemMod {
  string displayName { get; }
}

public static class ItemExtensions {
  public static string GetStatsFull(this Item item) {
    var text = item.GetStats() + "\n";
    if (item.mods.Any()) {
      text += $"<#FFFF00>{string.Join("\n", item.mods.Select(m => m.displayName))}</color>\n";
    }
    if (item is IWeapon w) {
      text += Util.DescribeDamageSpread(w.AttackSpread);
    }
    if (item is IDurable d) {
      text += $"Durability: {d.durability}/{d.maxDurability}.";
    }
    return text.Trim();
  }
}
