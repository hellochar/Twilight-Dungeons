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

  private int _stacks;
  public virtual int stacks {
    get => _stacks;
    set {
      if (value < 0) {
        throw new ArgumentException("Setting negative stack!" + this + " to " + value);
      }
      if (value > stacksMax) {
        value = stacksMax;
      }
      _stacks = value;
      if (_stacks == 0) {
        Destroy();
      }
    }
  }
  public virtual int stacksMax => 1;

  // An item is disjoint if it cannot be merged or accept new stacks from other instances of the same
  // item type.
  public virtual bool disjoint => false;

  public Item(int stacks) {
    this.stacks = stacks;
  }

  public Item() {
    this.stacks = disjoint ? stacksMax : 1;
  }

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

  public bool CanStackWith(Item other) {
    if (disjoint || other.disjoint || GetType() != other.GetType()) {
      return false;
    }
    return StackingPredicate(other);
  }

  // only called when other.GetType() == this.GetType()
  protected virtual bool StackingPredicate(Item other) {
    return true;
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
    if (item.disjoint) {
      text += $"Uses: {item.stacks}/{item.stacksMax}.";
    }
    return text.Trim();
  }

  /// Merge as many stacks as possible from other into this one.
  /// Return true if the other stack is now empty.
  /// NOTE: it's the responsibility of the caller to then dispose of the IStackable!
  public static bool Merge<T>(this T s, T other) where T : Item {
    var spaceLeft = s.stacksMax - s.stacks;
    var stacksToAdd = UnityEngine.Mathf.Clamp(other.stacks, 0, spaceLeft);
    s.stacks += stacksToAdd;
    other.stacks -= stacksToAdd;
    return other.stacks == 0;
  }
}
