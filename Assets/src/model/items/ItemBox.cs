using System;
using System.Collections.Generic;
using System.Reflection;

[Serializable]
[ObjectInfo("selection-circle")]
public class ItemBox : Item {
  public Item innerItem { get; protected set; }

  public ItemBox(Item innerItem, int stacks) : base(stacks) {
    this.innerItem = innerItem;
  }

  public ItemBox(Item innerItem) : this(innerItem, innerItem.stacks) {
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
public class ItemVisibleBox : ItemBox {
  public override string displayName => $"Essence of {innerItem.displayName}";
  public ItemVisibleBox(Item innerItem) : base(innerItem) {
  }

  internal override string GetStats() {
    return $"Unwrap at home to access the {innerItem.displayName}.";
  }

  public override int stacks {
    get {
      return innerItem.stacks;
    }

    set {
      if (innerItem != null) {
        innerItem.stacks = value;
      }
    }
  }

  public override int stacksMax => innerItem.stacksMax;

  protected override bool StackingPredicate(Item other) {
    var otherInnerItem = (other as ItemVisibleBox).innerItem;
    return innerItem.CanStackWith(otherInnerItem);
  }
}