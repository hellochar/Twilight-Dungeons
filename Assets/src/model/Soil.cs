using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ObjectInfo("soil", description: "Plant seeds here.", flavorText: "Fresh, moist, and perfect for growing. Hard to come by in the caves.")]
[Serializable]
public class Soil : Entity, IDaySteppable {
  private Vector2Int _pos;
  public override Vector2Int pos { get => _pos; set {} }
  // public static Vector2Int[] SoilShape = new Vector2Int[] {
  //   new Vector2Int(-1, -1),
  //   new Vector2Int(-1,  0),
  //   new Vector2Int(-1, +1),

  //   new Vector2Int(0, -1),
  //   new Vector2Int(0,  0),
  //   new Vector2Int(0, +1),

  //   new Vector2Int(+1, -1),
  //   new Vector2Int(+1,  0),
  //   new Vector2Int(+1, +1),
  // };
  // public override Vector2Int[] shape => SoilShape;
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
    player.UseResourcesOrThrow(organicMatter: 5);
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
public class ItemSoil : Item/*, ITargetedAction<Ground> */ {
  [PlayerAction]
  public void Sow() {
    var nearbySoils = GameModel.main.player.floor.soils.Where(soil => soil.IsDiagonallyNextTo(GameModel.main.player));
    if (nearbySoils.Any()) {
      throw new CannotPerformActionException("There is already soil nearby!");
    }
    // if (GameModel.main.player.tile.CanPlaceShape(Soil.SoilShape)) {
      SowPos(GameModel.main.player.pos);
    // } else {
    //   throw new CannotPerformActionException("Stand on a 3x3 empty patch of ground!");
    // }
  }

  public void SowPos(Vector2Int pos) {
    GameModel.main.player.floor.Put(new Soil(pos));
    Destroy();
  }

  // public string TargettedActionName => "Sow";
  // public string TargettedActionDescription => "Choose where to place Soil.";
  // public void PerformTargettedAction(Player player, Entity target) {
  //   player.UseResourcesOrThrow(0, 0, 1);
  //   // foreach (var tile in player.floor.GetAdjacentTiles(target.pos).ToList()) {
  //   //   if (tile.GetType() == typeof(Ground)) {
  //   //     player.floor.Put(new Soil(tile.pos));
  //   //     // player.floor.Put(new GrowingEntity(tile.pos, new Soil(tile.pos)));
  //   //   }
  //   // }
  //   Sow(target.pos);
  // }

  // public IEnumerable<Ground> Targets(Player player) {
  //   return player.GetVisibleTiles().Where(tile =>
  //     tile is Ground &&
  //     tile.CanPlaceShape(Soil.SoilShape)
  //   ).Cast<Ground>();
  // }
}