using System;
using UnityEngine;

// for now - 0 arg methods
internal class PlayerActionAttribute : Attribute {
}

[Serializable]
public abstract class Station : Body, IInteractableInventory {
  public Inventory inventory { get; }
  public Station(Vector2Int pos) : base(pos) {
    durability = maxDurability;
    inventory = new Inventory(1);
  }

  public int durability { get; set; }
  public abstract int maxDurability { get; }
  public abstract bool isActive { get; }
  public override string displayName => $"{base.displayName} {durability}/{maxDurability}";

  public void ReduceDurability() {
    durability--;
    if (durability <= 0) {
      KillSelf();
    }
  }
}

public interface IInteractableInventory {
  public Inventory inventory { get; }
}