using System;
using System.Collections;
using UnityEngine;

public class GameModel {
  public Player player;
  public Floor[] floors;
  public int activeFloorIndex = 0;

  public static GameModel model = GameModel.generateGameModel(); //new GameModel();
  public static GameModel generateGameModel() {
    GameModel model = new GameModel();
    model.floors = new Floor[] {
      // Floor.generateFloor0(),
      Floor.generateRandomFloor(),
      // Floor.generateRandomFloor(),
      // Floor.generateRandomFloor(),
      // Floor.generateRandomFloor(),
      // Floor.generateRandomFloor(),
    };

    Tile floor0Upstairs = model.floors[0].upstairs;
    model.player = new Player(new Vector2Int(floor0Upstairs.pos.x + 1, floor0Upstairs.pos.y));
    model.floors[0].entities.Add(model.player);
    return model;
  }
}
