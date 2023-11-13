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
    HUD.waterIndicator?.SetActive(false);

    Player player = GameModel.main.player;
    player.OnChangeWater += HandleChangeWater;                        // after getting water
  }

  bool tutorialStarted = false;
  async void TutorialFlow() {
    if (tutorialStarted) {
      return;
    }
    tutorialStarted = true;

    Messages.Create("Grow your garden!");

    Player player = GameModel.main.player;
    // while water is less than 100, get water.
    if (player.water < 100) {
      foreach (var water in player.floor.tiles.OfType<Water>()) {
        Highlights.Create(water, () => player.water >= 100);
      };
    }

    // initial berry bush
    var berryBush = GameModel.main.currentFloor.bodies.OfType<Plant>().FirstOrDefault();
    if (berryBush != null) {
      var highlight = Highlights.Create(berryBush);
      IEnumerator Tick() {
        while (highlight != null) {
          highlight.SetActive(player.water >= 100);
          yield return new WaitForSeconds(0.1f);
        }
      }
      StartCoroutine(Tick());
    }

    _ = Task.Run(async () => {
      var newPlant = await Util.WaitUntilNotNull(() => {
        var plant = GameModel.main.currentFloor.bodies.OfType<Plant>().FirstOrDefault();
        if (plant == berryBush) {
          return null;
        }
        return plant;
      });
      // we're done!
      // must call this from main thread
      queueCompleteLogic = true;
    });

    if (FloorController.current.TryGetControllerComponent<PlantController>(berryBush, out var plantController)) {
      Util.WheneverChanged(this,
        () => plantController.GetPopupPlantUIController(),
        (ui) => {
          if (ui == null) {
            return;
          }

          var harvest0Button = ui.harvests.transform.GetChild(0).Find("HarvestButton").gameObject;
          // // need to let plant UI layout
          // await Task.Delay(100);
          Highlights.CreateUI(harvest0Button);
        }
      );
    }

    // _ = Messages.CreateDelayed("Harvest the Berry Bush!", 1, 5);

    // // When the UI gets shown, add a pointer to the first harvest button
    // var plantUI = await Util.WaitUntilNotNull(() => PlantUIController.active);
    // var harvest0 = plantUI.harvests.transform.GetChild(0).Find("HarvestButton").gameObject;
    // Highlights.CreateUI(harvest0);

    // When the harvest happens, add a pointer to the seed
    // TODO handle player loading from save?
    var seed = await Util.WaitUntilNotNull(() =>
      GameModel.main.currentFloor.items.Where(i => i.item is ItemSeed).FirstOrDefault()
    );
    Highlights.Create(seed);

    // When the seed is picked up, add a pointer to the ItemSeed in the inventory
    var itemSeed = await Util.WaitUntilNotNull(() =>
      GameModel.main.player.inventory.OfType<ItemSeed>().FirstOrDefault()
    );
    var HUDItemSeed = HUDController.main.playerInventory.GetSlot(itemSeed);
    var itemSeedController = HUDItemSeed.GetComponentInChildren<ItemController>();
    {
      var highlight = Highlights.CreateUI(HUDItemSeed.gameObject, () => itemSeed.stacks < 2);
      IEnumerator TickHighlight() {
        while (highlight != null) {
          // when you're in the popup for the seed, hide the highlight
          bool popupActive = itemSeedController.popup != null;
          highlight.SetActive(!popupActive);
          yield return new WaitForSeconds(0.1f);

        }
      }
      StartCoroutine(TickHighlight());
    }

    Util.WheneverChanged(this,
      () => itemSeedController.popup,
      (GameObject popup) => {
        if (popup == null) {
          return;
        }

        // when item popup is shown, highlight the Plant action
        var plantButton = popup.transform.Find("Frame/Actions/Plant");
        Highlights.CreateUI(plantButton.gameObject);
      });

    // _ = Messages.CreateDelayed("Plant the seed!", 1, 5);

    // when the ItemSeed is tapped, add a pointer to the "Plant" button

    // When the plant button is pressed, add a pointer to a candidate tile
    // When the error shows up, add a pointer to a water tile
    // When you're over 100 water, add a pointer to the ItemSeed.
    // When the seed is planted, you're *done*. Show the downstairs
  }

  void LateUpdate() {
    // do this in first LateUpdate so that FloorController has a chance to create all GameObjects
    if (!tutorialStarted) {
      TutorialFlow();
    }
    if (queueCompleteLogic) {
      PlayerPrefs.SetInt(KEY_HAS_COMPLETED_GARDEN_TUTORIAL, 1);
      GameModel.main.currentFloor.AddDownstairs();
      GameModel.main.DrainEventQueue();
      Highlights.RemoveAll();
      _ = Messages.CreateDelayed("Have fun!", 1, 3);
      
      queueCompleteLogic = false;
    }
  }

  // private void HandleSeedPickup(Item item, Entity arg2) {
  //   if (item is ItemSeed) {
  //     GameModel.main.player.inventory.OnItemAdded -= HandleSeedPickup;
  //     Messages.Create("Plant the Seed!");
  //   }
  // }

  private void HandleChangeWater(int delta) {
    Transitions.AnimateUIHorizontally(HUDController.main.waterIndicator, 900);
    GameModel.main.player.OnChangeWater -= HandleChangeWater;
  }
}
