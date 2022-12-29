using System;
using System.Reflection;
using IngameDebugConsole;

public class PlayerConsoleMethods {

  [ConsoleMethod("CheatAddItemGrass", "Add an ItemGrass to player inventory")]
  public static void CheatAddItemGrass(string grassTypeStr, int stacks) {
    Type grassType = Assembly.GetExecutingAssembly().GetType(grassTypeStr);
    ItemGrass itemGrass = new ItemGrass(grassType, stacks);
    GameModel.main.player.inventory.AddItem(itemGrass);
  }
}