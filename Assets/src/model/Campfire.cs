using System;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo("campfire", description: "Heals you to full HP and removes all negative statuses.")]
public class Campfire : Body {
  [field:NonSerialized] /// controller-only
  public event Action OnHealed;
  public Campfire(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 10;
  }

  public void Heal() {
    Player p = GameModel.main.player;
    p.UseActionPointOrThrow();
    p.Heal(4);
    var debuffs = p.statuses.list.Where((s) => s.isDebuff);
    foreach (var debuff in debuffs) {
      p.statuses.Remove(debuff);
      break;
    }
    OnHealed?.Invoke();
  }
}

[Serializable]
[ObjectInfo("station", description: "Purify your slime here.")]
public class Desalinator : Body {
  public Desalinator(Vector2Int pos) : base(pos) {
  }

  public void Purify(ItemSlime slime) {
    slime.Purify(GameModel.main.player);
  }
}

[Serializable]
[ObjectInfo("station", description: "Build more shovels here.")]
public class CraftingStation : Body {
  public CraftingStation(Vector2Int pos) : base(pos) {}

  public void CraftShovel(Player player) {
    player.UseActionPointOrThrow();
    player.inventory.AddItem(new ItemShovel(), this);
  }
}

// Stations will provide access to verbs on Items and on yourself:
// Crafting the hea
// public class Station : Body {

// }