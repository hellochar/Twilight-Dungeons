using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectInfo {
  private static ObjectInfo DEFAULT = new ObjectInfo {
    spriteName = "colored_transparent_packed_1046",
    flavorText = "Missing Item info!"
  };

  private static readonly Dictionary<Type, ObjectInfo> Infos = new Dictionary<Type, ObjectInfo> {
    [typeof(ItemBarkShield)] = new ObjectInfo {
      spriteName = "colored_transparent_packed_134",
      flavorText = "Chunks of bark, twigs, and leaves are tightly bound together with sap and sinew into a makeshift shield. It'll protect you, but not for long."
    },
    [typeof(ItemBerries)] = new ObjectInfo {
      spriteName = "berry-red-1",
      flavorText = "Small but packed with goodness!",
    },
    [typeof(ItemSeed)] = new ObjectInfo {
      spriteName = "roguelikeSheet_transparent_532",
      flavorText = "But where shall I be,\nWhen this little Seed is a tall green Tree?"
    },
    [typeof(ItemHands)] = new ObjectInfo {
      showOnPlayer = false,
      spriteName = "Hands",
      flavorText = "Years of botany leave you with calloused, work-ready hands."
    },
    [typeof(ItemStick)] = new ObjectInfo {
      spriteName = "Stick",
      flavorText = "Stiff but brittle, this won't last long."
    },
    [typeof(ItemMushroom)] = new ObjectInfo {
      spriteName = "mushroom",
      flavorText = "At least it's not toxic!"
    },
    [typeof(SoftGrassStatus)] = new ObjectInfo {
      spriteName = "colored_transparent_packed_95",
      flavorText = "Feels nice on your feet."
    }
  };
  public static ObjectInfo InfoFor(object item) {
    return Infos.ContainsKey(item.GetType()) ? Infos[item.GetType()] : DEFAULT;
  }

  public static Sprite GetSpriteFor(object item) {
    return InfoFor(item).sprite;
  }

  public static string GetFlavorTextFor(object item) {
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
        if (_sprite == null) {
          Debug.LogWarning("Couldn't find sprite " + spriteName);
        }
      }
      return _sprite;
    }
  }
}