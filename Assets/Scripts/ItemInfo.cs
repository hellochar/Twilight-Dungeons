using System;
using System.Collections.Generic;

public static class ItemInfo {
  public static readonly Dictionary<Type, string> FlavorText = new Dictionary<Type, string> {
    [typeof(ItemBarkShield)] = "Chunks of bark, twigs, and leaves are tightly bound together with sap and sinew into a makeshift shield. It'll protect you, but not for long.",
    [typeof(ItemBerries)] = "Small but packed with goodness!",
    [typeof(ItemSeed)] = "But where shall I be,\nWhen this little Seed is a tall green Tree?"
  };
}