public interface IDurable {
  int durability { get; set; }
  int maxDurability { get; }
}

static class IDurableExtensions {
  public static void ReduceDurability(this IDurable durable) {
    durable.durability--;
    if (durable.durability <= 0 && durable is Item i) {
      i.Destroy(null);
    }
  }
}