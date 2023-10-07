using System;
using System.Linq;
using System.Reflection;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerConsoleMethods {

  // [ConsoleMethod("CAddItemGrass", "Add an ItemGrass to player inventory")]
  // public static void CheatAddItemGrass(string grassTypeStr, int stacks = 1) {
  //   Type grassType = Assembly.GetExecutingAssembly().GetType(grassTypeStr);
  //   ItemGrass itemGrass = new ItemGrass(grassType);
  //   GameModel.main.player.inventory.AddItem(itemGrass);
  // }

  [ConsoleMethod("CheatAddItem", "Add an Item to player inventory")]
  public static void CheatAddItem(string itemTypeStr, int num = 1) {
    Type itemType = Assembly.GetExecutingAssembly().GetType(itemTypeStr);
    var constructor0 = itemType.GetConstructor(new Type[0]);
    if (constructor0 != null) {
      for (int i = 0; i < num; i++) {
        var item = (Item)constructor0.Invoke(new object[0]);
        GameModel.main.player.inventory.AddItem(item);
      }
      return;
    }

    var constructor1 = itemType.GetConstructor(new Type[] { typeof(int) });
    if (constructor1 != null) {
      var item = (Item)constructor1.Invoke(new object[] { num });
      GameModel.main.player.inventory.AddItem(item);
      return;
    }
  }

  // [ConsoleMethod("CAddItemPlaceableEntity", "Add an ItemPlaceableEntity to player inventory")]
  // public static void CheatAddItemPlaceableEntity(string entityTypeStr, int stacks = 1) {
  //   // Type grassType = Assembly.GetExecutingAssembly().GetType(grassTypeStr);
  //   Type entityType = Assembly.GetExecutingAssembly().GetType(entityTypeStr);
  //   var constructor = entityType.GetConstructor(new Type[] { typeof(Vector2Int) });
  //   var entity = (Entity) constructor.Invoke(new object[] { new Vector2Int(0, 0) });
  //   if (entity is AIActor actor) {
  //     actor.SetAI(new WaitAI(actor));
  //     // actor.statuses.Add(new CharmedStatus());
  //     actor.faction = Faction.Ally;
  //   }
  //   ItemPlaceableEntity item = new ItemPlaceableEntity(entity);
  //   item.stacks = stacks;
  //   GameModel.main.player.inventory.AddItem(item);
  // }

  // [ConsoleMethod("CTriggerCaptureAction", "Trigger capture action")]
  // public static void CheatTriggerCaptureAction() {
  //   new CaptureAction().ShowTargetingUIThenPerform(GameModel.main.player);
  // }

  // [ConsoleMethod("CGetReward", "Show reward UI")]
  // public static void CheatGetRewardAsync() {
  //   // new CaptureAction().ShowTargetingUIThenPerform(GameModel.main.player);
  //   var _ = GameModel.main.currentFloor.CreateRewards().ShowRewardUIAndWaitForChoice();
  // }


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

// #if experimental_actionpoints
//   [ConsoleMethod("CAddActionPoint", "")]
//   public static void CheatAddActionPoint() {
//     GameModel.main.player.actionPoints++;
//   }
// #endif

  [ConsoleMethod("CheatMaturePlants", "")]
  public static void CheatMaturePlants() {
    foreach(var plant in GameModel.main.home.bodies.OfType<Plant>().ToList()) {
      plant.GoNextStage();
    }
  }

  // [ConsoleMethod("CGoNextDay", "")]
  // public static void CheatGoNextDay() {
  //   GameModel.main.GoNextDay();
  // }

  private static GameObject canvas;
  [ConsoleMethod("ToggleHUD", "")]
  public static void ToggleHUD() {
    if (canvas == null) {
      canvas = GameObject.Find("Canvas");
    }
    canvas.SetActive(!canvas.activeSelf);
  }

  [ConsoleMethod("CheatAddEncounter", "")]
  public static void DoEncounter(string name) {
    Encounters.GetByName(name)?.Invoke(GameModel.main.player.floor, GameModel.main.player.floor.root);
  }
}