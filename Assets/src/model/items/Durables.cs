public static class Durables {
  public static void ReduceDurability(IDurable durable) {
    durable.durability--;
    if (durable.durability <= 0 && durable is Item i) {
      i.Destroy(null);
    }
  }
}
