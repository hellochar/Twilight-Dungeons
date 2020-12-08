public static class ItemUtils {
  public static string GetStatsFull(this Item item) {
    var text = item.GetStats();
    if (item is IWeapon w) {
      var (min, max) = w.AttackSpread;
      text += $"\n{min} - {max} damage.";
    }
    if (item is IDurable d) {
      text += $"\nDurability: {d.durability}/{d.maxDurability}.";
    }
    return text.Trim();
  }
}