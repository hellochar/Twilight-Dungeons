using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Walk over it to harvest.")]
public class Mushroom : Grass/*, IActorEnterHandler*/ {
  public static Item HomeItem => new ItemMushroom(4);
  public Mushroom(Vector2Int pos) : base(pos) {
  }

  // public void HandleActorEnter(Actor actor) {
  //   var player = GameModel.main.player;
  //   if (actor == player) {
  //     BecomeItemInInventory(new ItemMushroom(1), player);
  //   }
  // }

  public static bool CanOccupy(Tile tile) {
    var floor = tile.floor;
    // hugging at least one 4-neighbor wall
    var cardinalNeighbors = floor.GetCardinalNeighbors(tile.pos);
    var isHuggingWall = cardinalNeighbors.Any((pos) => pos is Wall);
    var isGround = tile is Ground;
    var isNotOccupied = tile.grass == null;
    
    return isHuggingWall && isGround && isNotOccupied;
  }
}