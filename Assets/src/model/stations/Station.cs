using System;
using UnityEngine;

// for now - 0 arg methods
internal class PlayerActionAttribute : Attribute {
}

[Serializable]
public abstract class Station : Body, IDurable {
  public Station(Vector2Int pos) : base(pos) {
    durability = maxDurability;
  }

  public int durability { get; set; }

  public abstract int maxDurability { get; }
  public override string displayName => $"{base.displayName} {durability}/{maxDurability}";
}
