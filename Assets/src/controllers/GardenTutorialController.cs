using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GardenTutorialController : MonoBehaviour {
  public static string KEY_HAS_COMPLETED_GARDEN_TUTORIAL = "hasCompletedGardenTutorial";
  private bool queueCompleteLogic;

  public static bool ShouldShow => !PlayerPrefs.HasKey(KEY_HAS_COMPLETED_GARDEN_TUTORIAL);
  public static void Complete() {
  }

  public void Start() {
    var HUD = HUDController.main;
    // HUD.hpBar?.SetActive(false);
    // HUD.statuses?.SetActive(false);
    HUD.waterIndicator?.SetActive(false);
    // HUD.inventoryToggle?.SetActive(false);
    // HUD.inventoryContainer?.SetActive(false);
    // // HUD.depth?.SetActive(false);
    // HUD.enemiesLeft?.SetActive(false);
    // // HUD.waitButton?.SetActive(false);
    // HUD.settings?.SetActive(false);

    Player player = GameModel.main.player;

    player.nonserializedModifiers.Add(this);                          // getting a status and healing
    // player.inventory.OnItemAdded += HandleFirstItemAdded;             // redberries
    // GameModel.main.turnManager.OnStep += DetectJackalsVisible;        // jackal room
    player.OnChangeWater += HandleChangeWater;                        // after getting water

    // on load, add a pointer to the berry bush.
    async void TutorialFlow() {
      var berryBush = GameModel.main.currentFloor.bodies.OfType<Plant>().FirstOrDefault();
      Highlights.Create(berryBush);
      // _ = Messages.CreateDelayed("Harvest the Berry Bush!", 1, 5);

      // // When the UI gets shown, add a pointer to the first harvest button
      // var plantUI = await Util.WaitUntilNotNull(() => PlantUIController.active);
      // var harvest0 = plantUI.harvests.transform.GetChild(0).Find("HarvestButton").gameObject;
      // Highlights.CreateUI(harvest0);

      // When the harvest happens, add a pointer to the seed
      var seed = await Util.WaitUntilNotNull(() =>
        GameModel.main.currentFloor.items.Where(i => i.item is ItemSeed).FirstOrDefault()
      );
      Highlights.Create(seed);

      _ = Task.Run(async () => {
        var newPlant = await Util.WaitUntilNotNull(() => GameModel.main.currentFloor.bodies.OfType<Plant>().FirstOrDefault());
        // we're done!
        // must call this from main thread
        queueCompleteLogic = true;
      });

      // When the seed is picked up, add a pointer to the ItemSeed in the inventory
      var itemSeed = await Util.WaitUntilNotNull(() =>
        GameModel.main.player.inventory.OfType<ItemSeed>().FirstOrDefault()
      );
      var HUDItemSeed = HUDController.main.playerInventory.GetSlot(itemSeed);
      Highlights.CreateUI(HUDItemSeed.gameObject, () => itemSeed.stacks < 2);

      // _ = Messages.CreateDelayed("Plant the seed!", 1, 5);

      // when the ItemSeed is tapped, add a pointer to the "Plant" button

      // When the plant button is pressed, add a pointer to a candidate tile
      // When the error shows up, add a pointer to a water tile
      // When you're over 100 water, add a pointer to the ItemSeed.
      // When the seed is planted, you're *done*. Show the downstairs
    }
    TutorialFlow();
  }

  void Update() {
    if (queueCompleteLogic) {
      PlayerPrefs.SetInt(KEY_HAS_COMPLETED_GARDEN_TUTORIAL, 1);
      GameModel.main.currentFloor.MaybeAddDownstairs();
      GameModel.main.DrainEventQueue();
      Highlights.RemoveAll();
      _ = Messages.CreateDelayed("Have fun!", 1, 3);
      
      queueCompleteLogic = false;
    }
  }

  private void HandleSeedPickup(Item item, Entity arg2) {
    if (item is ItemSeed) {
      GameModel.main.player.inventory.OnItemAdded -= HandleSeedPickup;
      Messages.Create("Plant the Seed!");
    }
  }

  private void HandleChangeWater(int delta) {
    Transitions.AnimateUIHorizontally(HUDController.main.waterIndicator, -900);
    GameModel.main.player.OnChangeWater -= HandleChangeWater;
  }
}
