using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ObjectInfo("soil", description: "Plant seeds here.", flavorText: "Fresh, moist, and perfect for growing. Hard to come by in the caves.")]
[Serializable]
public class Soil : Entity, IDaySteppable {
  private Vector2Int _pos;
  public override Vector2Int pos { get => _pos; set {} }
  public static Vector2Int[] SoilShape = new Vector2Int[] {
    new Vector2Int(-1, -1),
    new Vector2Int(-1,  0),
    new Vector2Int(-1, +1),

    new Vector2Int(0, -1),
    new Vector2Int(0,  0),
    new Vector2Int(0, +1),

    new Vector2Int(+1, -1),
    new Vector2Int(+1,  0),
    new Vector2Int(+1, +1),
  };
  public override Vector2Int[] shape => SoilShape;
  public Soil(Vector2Int pos) {
    _pos = pos;
  }

  public bool watered = false;
  public int nutrient = 0;
  public override string description => $"{(watered ? "Watered" : "Not Watered")}. {nutrient} nutrients.";

  [PlayerAction]
  public void Water() {
    var player = GameModel.main.player;
    player.UseResourcesOrThrow(water: 25);
    // player.UseActionPointOrThrow();
    watered = true;
  }

  [PlayerAction]
  public void Fertilize() {
    var player = GameModel.main.player;
    if (nutrient >= 5) {
      throw new CannotPerformActionException("Already at max nutrients!");
    }
    player.UseResourcesOrThrow(organicMatter: 1);
    nutrient++;
  }

  public void StepDay() {
    watered = false;
    if (nutrient > 0) {
      nutrient--;
    }
  }
}


[Serializable]
[ObjectInfo("soil", description: "Place in your home floor.")]
public class ItemSoil : Item, ITargetedAction<Ground> {
  public string TargettedActionName => "Sow";
  public string TargettedActionDescription => "Choose where to place Soil.";

  public void PerformTargettedAction(Player player, Entity target) {
    player.UseActionPointOrThrow();
    // foreach (var tile in player.floor.GetAdjacentTiles(target.pos).ToList()) {
    //   if (tile.GetType() == typeof(Ground)) {
    //     player.floor.Put(new Soil(tile.pos));
    //     // player.floor.Put(new GrowingEntity(tile.pos, new Soil(tile.pos)));
    //   }
    // }
    player.floor.Put(new Soil(target.pos));
    Destroy();
  }

  public IEnumerable<Ground> Targets(Player player) {
    return player.GetVisibleTiles().Where(tile =>
      tile.GetType() == typeof(Ground) &&
      tile.CanBeOccupied()
    ).Cast<Ground>();
  }
}