using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class Mushroom : Grass {
  public Mushroom(Vector2Int pos) : base(pos) {
    timeNextAction = this.timeCreated + GetRandomDuplicateTime();
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
  }

  void HandleEnterFloor() {
    tile.OnActorEnter += HandleActorEnter;
  }

  void HandleLeaveFloor() {
    tile.OnActorEnter -= HandleActorEnter;
  }

  private void HandleActorEnter(Actor actor) {
    if (actor == GameModel.main.player) {
      GameModel.main.player.inventory.AddItem(new ItemMushroom(1), this);
      Kill();
    }
  }

  private float GetRandomDuplicateTime() {
    return UnityEngine.Random.Range(66, 88);
  }

  public override float Step() {
    // find an adjacent square without mushrooms and grow into it
    var growTiles = floor.GetCardinalNeighbors(pos).Where(CanOccupy);
    if (growTiles.Any()) {
      var toGrowInto = Util.RandomPick(growTiles);
      var newMushrom = new Mushroom(toGrowInto.pos);
      floor.Put(newMushrom);
      TriggerNoteworthyAction();
    }
    return GetRandomDuplicateTime();
  }

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