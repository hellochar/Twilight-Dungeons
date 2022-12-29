using System;
using System.Linq;
using System.Reflection;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerConsoleMethods {

  [ConsoleMethod("CheatAddItemGrass", "Add an ItemGrass to player inventory")]
  public static void CheatAddItemGrass(string grassTypeStr, int stacks = 1) {
    Type grassType = Assembly.GetExecutingAssembly().GetType(grassTypeStr);
    ItemGrass itemGrass = new ItemGrass(grassType, stacks);
    GameModel.main.player.inventory.AddItem(itemGrass);
  }

  [ConsoleMethod("CheatAddVisibility", "Force add visibility")]
  public static void CheatAddVisibility() {
    GameModel.main.player.floor.ForceAddVisibility();
  }

  [ConsoleMethod("CheatNewGameTutorial", "")]
  public static void CheatNewGameTutorial() {
    GameModel.GenerateTutorialAndSetMain();
    SceneManager.LoadSceneAsync("Scenes/Game");
  }

  [ConsoleMethod("CheatAddWater", "")]
  public static void CheatAddWater(int water = 1000) {
    GameModel.main.player.water += water;
  }

  [ConsoleMethod("CheatMaturePlants", "")]
  public static void CheatMaturePlants() {
    foreach(var plant in GameModel.main.home.plants.ToList()) {
      plant.GoNextStage();
    }
  }

  [ConsoleMethod("CheatGoNextDay", "")]
  public static void CheatGoNextDay() {
    if (GameModel.main.player.floor is HomeFloor f) {
      GameModel.main.GoNextDay();
    }
  }

  [ConsoleMethod("ToggleHUD", "")]
  public static void ToggleHUD() {
    var canvas = GameObject.Find("Canvas");
    canvas.SetActive(!canvas.activeSelf);
  }
}