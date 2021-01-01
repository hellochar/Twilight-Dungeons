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
    [typeof(ItemVineWhip)] = new ObjectInfo {
      spriteName = "hanging-vines-bottom",
      flavorText = "Just the sound of it whipping through the air makes you a little nervous."
    },
    [typeof(SoftGrassStatus)] = new ObjectInfo {
      spriteName = "colored_transparent_packed_95",
      flavorText = "Feels nice on your feet."
    },
    [typeof(WebStatus)] = new ObjectInfo {
      spriteName = "web",
      flavorText = "Sticky, gooey, and warm. Eeeew."
    },
    [typeof(SlimedStatus)] = new ObjectInfo {
      spriteName = "slimed",
      flavorText = "Sticky, gooey, and warm. Eeeew."
    },
    [typeof(PoisonedStatus)] = new ObjectInfo {
      spriteName = "poisoned",
      flavorText = "You feel sick to your stomach, but appreciate the dangerous beauty in nature."
    },
    [typeof(BoundStatus)] = new ObjectInfo {
      spriteName = "bound-status",
      flavorText = "Thick, damp vines entangle you!"
    },
    [typeof(FrenziedStatus)] = new ObjectInfo {
      spriteName = "3Red",
      flavorText = "You're engulfed in a rage!"
    },
    [typeof(StatusWild)] = new ObjectInfo {
      spriteName = "wildwood-leaf",
      flavorText = "This place feels tight and clausterphobic!"
    },
  };
  public static ObjectInfo InfoFor(object item) {
    var type = item.GetType();
    if (!Infos.ContainsKey(type)) {
      // try to load it from the attribute
      var objectInfoArray = type.GetCustomAttributes(typeof(ObjectInfoAttribute), false);
      if (objectInfoArray.Length > 0) {
        var attribute = (ObjectInfoAttribute) objectInfoArray[0];
        Infos[type] = new ObjectInfo {
          spriteName = attribute.spriteName,
          flavorText = attribute.flavorText
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

  public bool showOnPlayer = true;
  public string spriteName;
  public string flavorText;
  private Sprite _sprite;
  public Sprite sprite {
    get {
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
  public string flavorText {get; }

  public ObjectInfoAttribute(string spriteName, string flavorText) {
    this.spriteName = spriteName;
    this.flavorText = flavorText;
  }
}
