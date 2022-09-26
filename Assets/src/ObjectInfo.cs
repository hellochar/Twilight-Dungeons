using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectInfo {
  private static ObjectInfo DEFAULT = new ObjectInfo {
    spriteName = "colored_transparent_packed_1046",
    flavorText = "",
    description = ""
  };

  private static readonly Dictionary<Type, ObjectInfo> Infos = new Dictionary<Type, ObjectInfo> {
    [typeof(ItemBarkShield)] = new ObjectInfo {
      spriteName = "colored_transparent_packed_134",
      flavorText = "Chunks of bark, twigs, and leaves are tightly bound together with sap and sinew into a makeshift shield. It'll protect you, but not for long."
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
    [typeof(ItemDeathbloomFlower)] = new ObjectInfo {
      spriteName = "3Red",
      flavorText = "The rare Deathbloom flower. Just the smell of it gets your blood pumping."
    },
    [typeof(ItemWildwoodWreath)] = new ObjectInfo {
      spriteName = "wildwood-wreath",
      flavorText = "Leaves arranged in a circular pattern, to be worn on your head."
    },
    [typeof(ItemWildwoodLeaf)] = new ObjectInfo {
      spriteName = "wildwood-leaf",
      flavorText = "Named for its ability to thrive in harsh conditions, this blueish leaf has a variety of uses."
    },
    [typeof(WebbedStatus)] = new ObjectInfo {
      spriteName = "web",
      flavorText = "Sticky, gooey, and warm. Eeeew."
    },
    [typeof(SlimedStatus)] = new ObjectInfo {
      spriteName = "slimed",
      flavorText = "Sticky, gooey, and warm. Eeeew."
    },
    [typeof(StatusWild)] = new ObjectInfo {
      spriteName = "wildwood-leaf",
      flavorText = "This place feels tight and clausterphobic!"
    },
  };
  public static ObjectInfo InfoFor(object item) {
    if (item is ItemPlaceableEntity e) {
      return InfoFor(e.entity);
    }
    if (item is ItemVisibleBox b) {
      return InfoFor(b.innerItem);
    }
    var type = item is Type t ? t : item.GetType();
    if (!Infos.ContainsKey(type)) {
      // try to load it from the attribute
      var objectInfoArray = type.GetCustomAttributes(typeof(ObjectInfoAttribute), false);
      if (objectInfoArray.Length > 0) {
        var attribute = (ObjectInfoAttribute) objectInfoArray[0];
        Infos[type] = new ObjectInfo {
          spriteName = attribute.spriteName,
          flavorText = attribute.flavorText,
          description = attribute.description
        };
      } else {
        Infos[type] = DEFAULT;
      }
    }
    return Infos[type];
  }

  public static Sprite GetSpriteFor(object item) {
    return InfoFor(item).sprite;
  }

  public static string GetFlavorTextFor(object item) {
    return InfoFor(item).flavorText;
  }
  
  public static string GetDescriptionFor(object item) {
    return InfoFor(item)?.description ?? $"Description missing for {item.GetType()}";
  }

  public bool showOnPlayer = true;
  public string spriteName;
  public string flavorText;
  public string description;
  private Sprite _sprite;
  public Sprite sprite {
    get {
      if (spriteName == null) {
        return null;
      }
      if (_sprite == null) {
        _sprite = MasterSpriteAtlas.atlas.GetSprite(spriteName);
        if (_sprite == null) {
          Debug.LogWarning("Couldn't find sprite in Atlas " + spriteName);
          _sprite = Resources.Load<Sprite>(spriteName);
          if (_sprite == null) {
            Debug.LogWarning("Couldn't find sprite Resource " + spriteName);
          }
        }
      }
      return _sprite;
    }
  }
}

public class ObjectInfoAttribute : Attribute {
  public string spriteName { get; }
  public string flavorText { get; }
  public string description { get; }

  public ObjectInfoAttribute(string spriteName = null, string flavorText = "", string description = "") {
    this.spriteName = spriteName;
    this.flavorText = flavorText;
    this.description = description;
  }
}
