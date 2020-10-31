using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class GameModel {
  public Player player;
  public Floor[] floors;
  public int activeFloorIndex = 0;

  public static GameModel main = GameModel.generateGameModel(); //new GameModel();

  public Floor currentFloor {
    get {
      return floors[activeFloorIndex];
    }
  }

  public static GameModel generateGameModel() {
    GameModel model = new GameModel();
    model.floors = new Floor[] {
      Floor.generateFloor0(),
      Floor.generateRandomFloor(),
      Floor.generateRandomFloor(),
      // Floor.generateRandomFloor(),
      // Floor.generateRandomFloor(),
      // Floor.generateRandomFloor(),
    };

    Tile floor0Upstairs = model.floors[0].upstairs;
    model.player = new Player(new Vector2Int(floor0Upstairs.pos.x + 1, floor0Upstairs.pos.y));
    // model.floors[0].actors.Add(model.player);
    model.floors[0].AddVisibility(model.player);
    return model;
  }

  internal void PutPlayerAt(Floor nextFloor, bool isGoingUpstairs) {
    // Update active floor index
    // Put Player in new position after finding the connecting downstairs/upstairs
    // deactivate current floor
    int newIndex = Array.FindIndex(floors, f => f == nextFloor);
    this.activeFloorIndex = newIndex;
    Vector2Int newPlayerPosition;
    if (isGoingUpstairs) {
      newPlayerPosition = this.currentFloor.downstairs.pos + new Vector2Int(-1, 0);
    } else {
      newPlayerPosition = this.currentFloor.upstairs.pos + new Vector2Int(1, 0);
    }
    player.pos = newPlayerPosition;
  }
}
