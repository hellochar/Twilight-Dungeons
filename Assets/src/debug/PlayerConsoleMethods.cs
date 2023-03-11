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

  [ConsoleMethod("CheatAddItem", "Add an Item to player inventory")]
  public static void CheatAddItem(string itemTypeStr) {
    Type itemType = Assembly.GetExecutingAssembly().GetType(itemTypeStr);
    var constructor = itemType.GetConstructor(new Type[0]);
    var item = (Item)constructor.Invoke(new object[0]);
    GameModel.main.player.inventory.AddItem(item);
  }

  [ConsoleMethod("CheatAddItemPlaceableEntity", "Add an ItemPlaceableEntity to player inventory")]
  public static void CheatAddItemPlaceableEntity(string entityTypeStr, int stacks = 1) {
    // Type grassType = Assembly.GetExecutingAssembly().GetType(grassTypeStr);
    Type entityType = Assembly.GetExecutingAssembly().GetType(entityTypeStr);
    var constructor = entityType.GetConstructor(new Type[] { typeof(Vector2Int) });
    var entity = (Entity) constructor.Invoke(new object[] { new Vector2Int(0, 0) });
    if (entity is AIActor actor) {
      actor.SetAI(new WaitAI(actor));
      // actor.statuses.Add(new CharmedStatus());
      actor.faction = Faction.Ally;
    }
    ItemPlaceableEntity item = new ItemPlaceableEntity(entity);
    item.stacks = stacks;
    GameModel.main.player.inventory.AddItem(item);
  }

  [ConsoleMethod("CheatTriggerCaptureAction", "Trigger capture action")]
  public static void CheatTriggerCaptureAction() {
    new CaptureAction().ShowTargetingUIThenPerform(GameModel.main.player);
  }

  [ConsoleMethod("CheatGetReward", "Show reward UI")]
  public static void CheatGetReward() {
    // new CaptureAction().ShowTargetingUIThenPerform(GameModel.main.player);
    GameModel.main.currentFloor.CreateRewards().ShowRewardUIAndWaitForChoice();
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
  public static void CheatAddWater(int water = 100) {
    GameModel.main.player.water += water;
  }

#if experimental_actionpoints
  [ConsoleMethod("CheatAddActionPoint", "")]
  public static void CheatAddActionPoint() {
    GameModel.main.player.actionPoints++;
  }
#endif

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

  private static GameObject canvas;
  [ConsoleMethod("ToggleHUD", "")]
  public static void ToggleHUD() {
    if (canvas == null) {
      canvas = GameObject.Find("Canvas");
    }
    canvas.SetActive(!canvas.activeSelf);
  }
}