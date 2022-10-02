using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ObjectInfo("soil", description: "Plant seeds here.", flavorText: "Fresh, moist, and perfect for growing. Hard to come by in the caves.")]
[Serializable]
public class Soil : Ground {
  public Soil(Vector2Int pos) : base(pos) { }
}


[Serializable]
[ObjectInfo("soil", description: "Place in your home floor.")]
public class ItemSoil : Item, ITargetedAction<Ground> {
  public string TargettedActionName => "Sow";

  public void PerformTargettedAction(Player player, Entity target) {
    player.UseActionPointOrThrow();
    foreach (var tile in player.floor.GetAdjacentTiles(target.pos).ToList()) {
      if (tile.GetType() == typeof(Ground)) {
        // player.floor.Put(new Soil(tile.pos));
        player.floor.Put(new GrowingEntity(tile.pos, new Soil(tile.pos)));
      }
    }
    Destroy();
  }

  public IEnumerable<Ground> Targets(Player player) {
    return player.GetVisibleTiles().Where(tile =>
      tile.GetType() == typeof(Ground) &&
      tile.CanBeOccupied()
    ).Cast<Ground>();
  }
}