using UnityEngine;

public interface IDurable {
  /// do not set directly; use ReduceDurability()
  int durability { get; set; }
  int maxDurability { get; }
}

static class IDurableExtensions {
  public static void ReduceDurability(this IDurable durable) {
    durable.durability--;
    if (durable.durability <= 0 && durable is Item i) {
      i.Destroy();
    }
  }

  public static void IncreaseDurability(this IDurable durable, int amount = 1) {
    durable.durability = Mathf.Min(durable.durability + amount, durable.maxDurability);
  }
}