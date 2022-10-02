using System;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo("fountain", description: "Purify your slime here.")]
public class Desalinator : Station {
  public override int maxDurability => 7;
  public Desalinator(Vector2Int pos) : base(pos) { }

  [PlayerAction]
  public void Purify() {
    var player = GameModel.main.player;
    var slime = player.inventory.FirstOrDefault(i => i is ItemSlime) as ItemSlime;
    if (slime != null) {
      slime.Purify(player);
      this.ReduceDurability();
    }
  }
}
