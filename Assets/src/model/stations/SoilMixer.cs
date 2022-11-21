using System;
using UnityEngine;

[Serializable]
[ObjectInfo("fountain", description: "Create soil for 100 Water, 10 Organic Matter, and 1 AP.")]
public class SoilMixer : Station {
  public override int maxDurability => 9;

  public override bool isActive => false;
  public SoilMixer(Vector2Int pos) : base(pos) {}

  [PlayerAction]
  public void Mix() {
    var player = GameModel.main.player;
    player.UseResourcesOrThrow(100, 10, 1);
    var soil = new ItemSoil();
    if (!player.inventory.AddItem(soil)) {
      player.floor.Put(new ItemOnGround(pos, soil));
    }
  }
}