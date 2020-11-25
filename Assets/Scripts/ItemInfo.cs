using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemInfo {
  private static ItemInfo DEFAULT = new ItemInfo {
    spriteName = "colored_transparent_packed_1046",
    flavorText = "Missing Item info!"
  };

  private static readonly Dictionary<Type, ItemInfo> Infos = new Dictionary<Type, ItemInfo> {
    [typeof(ItemBarkShield)] = new ItemInfo {
      spriteName = "colored_transparent_packed_134",
      flavorText = "Chunks of bark, twigs, and leaves are tightly bound together with sap and sinew into a makeshift shield. It'll protect you, but not for long."
    },
    [typeof(ItemBerries)] = new ItemInfo {
      spriteName = "berry-red-1",
      flavorText = "Small but packed with goodness!",
    },
    [typeof(ItemSeed)] = new ItemInfo {
      spriteName = "roguelikeSheet_transparent_532",
      flavorText = "But where shall I be,\nWhen this little Seed is a tall green Tree?"
    },
    [typeof(ItemHands)] = new ItemInfo {
      showOnPlayer = false,
      spriteName = "Hands",
      flavorText = "Years of botany leave you with calloused, work-ready hands."
    },
    [typeof(ItemStick)] = new ItemInfo {
      spriteName = "Stick",
      flavorText = "Stiff but brittle, this won't last long."
    }
  };
  public static ItemInfo InfoFor(Item item) {
    return Infos.ContainsKey(item.GetType()) ? Infos[item.GetType()] : DEFAULT;
  }

  public static Sprite GetSpriteForItem(Item item) {
    return InfoFor(item).sprite;
  }

  public static string GetFlavorTextForItem(Item item) {
    return InfoFor(item).flavorText;
  }

  public bool showOnPlayer = true;
  public string spriteName;
  public string flavorText;
  private Sprite _sprite;
  public Sprite sprite {
    get {
      if (_sprite == null) {
        _sprite = MasterSpriteAtlas.atlas.GetSprite(spriteName);
      }
      return _sprite;
    }
  }
}