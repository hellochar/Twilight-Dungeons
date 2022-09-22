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
    p.Heal(p.maxHp - p.hp);
    var debuffs = p.statuses.list.Where((s) => s.isDebuff);
    foreach (var debuff in debuffs) {
      p.statuses.Remove(debuff);
    }
    OnHealed?.Invoke();
  }
}