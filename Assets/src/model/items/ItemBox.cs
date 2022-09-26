using System;
using System.Collections.Generic;
using System.Reflection;

[Serializable]
[ObjectInfo("selection-circle")]
public class ItemBox : Item {
  public Item innerItem { get; protected set; }

  public ItemBox(Item innerItem) {
    this.innerItem = innerItem;
  }

  public virtual void Unwrap(Player p) {
    p.UseActionPointOrThrow();
    var inventory = this.inventory;
    var slot = inventory.GetSlotFor(this);
    Destroy();
    inventory.AddItem(innerItem, slot);
  }

  public override List<MethodInfo> GetAvailableMethods(Player player) {
    var methods = base.GetAvailableMethods(player);
    if (player.floor.depth == 0) {
      methods.Add(GetType().GetMethod("Unwrap"));
    }
    return methods;
  }
}

[Serializable]
public class ItemVisibleBox : ItemBox, IConditionallyStackable {
  public override string displayName => $"Essence of {innerItem.displayName}";
  public ItemVisibleBox(Item innerItem) : base(innerItem) {
  }

  internal override string GetStats() {
    return $"Unwrap at home to access the {innerItem.displayName}.";
  }

  int IStackable.stacks {
    get {
      if (innerItem is IStackable i) {
        return i.stacks;
      }
      return 1;
    }

    set {
      if (innerItem is IStackable i) {
        i.stacks = value;
      } else {
        throw new Exception("Setting stack on non-stackable inner item!");
      }
    }
  }
  int IStackable.stacksMax => (innerItem is IStackable i) ? i.stacksMax : 1;
  bool IConditionallyStackable.CanStackWith(IConditionallyStackable otherBox) {
    var other = (otherBox as ItemVisibleBox).innerItem;
    if (!(innerItem is IStackable)) {
      return false;
    }
    if (other.GetType() != innerItem.GetType()) {
      return false;
    }
    if (innerItem is IConditionallyStackable s1 && other is IConditionallyStackable s2) {
      return s2.CanStackWith(s1);
    }
    return true;
  }
}