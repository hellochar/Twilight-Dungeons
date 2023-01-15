using System;
using UnityEngine;

[Serializable]
// [ObjectInfo("fountain", description: "Create soil for 100 Water, 10 Organic Matter, and 1 AP.")]
[ObjectInfo("fountain", description: "Create 1 HomeGround for 25 Water and 1 Organic Matter, and 1 AP.")]
public class SoilMixer : Station {
  public override int maxDurability => 9;

  public override bool isActive => false;
  public SoilMixer(Vector2Int pos) : base(pos) {}

  [PlayerAction]
  public void Mix() {
    var player = GameModel.main.player;
    player.UseResourcesOrThrow(25, 1, 1);
    // var soil = new ItemSoil();
    // if (!player.inventory.AddItem(soil)) {
    //   player.floor.Put(new ItemOnGround(pos, soil));
    // }
    var homeGround = new ItemPlaceableTile(typeof(HomeGround), 1);
    if (!player.inventory.AddItem(homeGround)) {
      player.floor.Put(new ItemOnGround(pos, homeGround));
    }
  }
}