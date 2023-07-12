using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialController : MonoBehaviour, IStatusAddedHandler, IHealHandler {
  // Start is called before the first frame update
  public void Start() {
    // hide all the UI by default
    // hpBar = canvas.transform.Find("HUD/Hearts");
    // statuses = canvas.transform.Find("HUD/Statuses");
    // waterIndicator = GameObject.Find("Water Indicator");
    // inventoryToggle = GameObject.Find("Inventory Toggle");
    // inventoryContainer = GameObject.Find("Inventory Container");
    // depth = GameObject.Find("Depth");
    // enemiesLeft = GameObject.Find("Enemies Left");
    // waitButton = GameObject.Find("Wait Button");
    var HUD = HUDController.main;
    HUD.hpBar?.SetActive(false);
    HUD.statuses?.SetActive(false);
    HUD.waterIndicator?.SetActive(false);
    HUD.inventoryToggle?.SetActive(false);
    HUD.inventoryContainer?.SetActive(false);
    HUD.depth?.SetActive(false);
    HUD.enemiesLeft?.SetActive(false);
    HUD.waitButton?.SetActive(false);

    // AddHighlights();

    Player player = GameModel.main.player;

    // the order of these statements follows the order in which the player will hit them in the tutorial
    StartTutorial();
    player.nonserializedModifiers.Add(this);                          // getting a status and healing
    GameModel.main.turnManager.OnStep += DetectJackalsVisible;        // jackal room
    // GameModel.main.turnManager.OnStep += DetectEnteredBerryBushRoom;  // berry bush
    // player.inventory.OnItemAdded += HandleFirstItemAdded;             // after harvesting and picking up the first item
    // player.inventory.OnItemAdded += HandleSeedPickup;       // after picking up all 4 items
    // player.OnChangeWater += HandleChangeWater;                        // after getting water
    // GameModel.main.turnManager.OnStep += DetectEnteredFinalRoom;      // final room
    TutorialFloor.OnTutorialEnded += HandleTutorialEnded;                  // end!
  }

  // void AddHighlights() {
  //   PrefabCache.Effects.Instantiate("Highlight", GameObjectFor(tutFloor.blob).transform);
  //   PrefabCache.Effects.Instantiate("Highlight", GameObjectFor(tutFloor.guardleaf).transform);
  //   PrefabCache.Effects.Instantiate("Highlight", GameObjectFor(tutFloor.jackals[0]).transform);
  //   PrefabCache.Effects.Instantiate("Highlight", GameObjectFor(tutFloor.berryBush).transform);
  //   PrefabCache.Effects.Instantiate("Highlight", GameObjectFor(tutFloor.astoria).transform);
  //   PrefabCache.Effects.Instantiate("Highlight", GameObjectFor(tutFloor.bat).transform);
  // }

  void StartTutorial() {
    StartCoroutine(DelayedMessage());
    IEnumerator DelayedMessage() {
      yield return new WaitForSeconds(1f);
      Messages.Create("Tap to move.", 5);
    }
  }

  public void HandleHeal(int amount) {
    if (!HUDController.main.hpBar.activeSelf) {
      AnimateHorizontally(HUDController.main.hpBar, 900);
      // GameModel.main.player.AddTimedEvent(2, () => AnimateHorizontally(hpBar, 900));
    }
  }

  // /// show HP, and explain "tap hold"
  // void DetectBlobVisible(ISteppable _) {
  //   if (!tutFloor.blob.isVisible) {
  //     return;
  //   }

  //   GameModel.main.turnManager.OnStep -= DetectBlobVisible;
  //   GameModel.main.player.ClearTasks();

  //   // 900px is Canvas's canvas scalar reference resolution
  //   StartCoroutine(DelayedMessage());
  //   IEnumerator DelayedMessage() {
  //     yield return new WaitForSeconds(0.25f);
  //     Messages.Create("Tap on things to learn about them.", 5);
  //   }
  // }

  public void HandleStatusAdded(Status status) {
    if (!HUDController.main.statuses.activeSelf) {
      AnimateHorizontally(HUDController.main.statuses, 900);
    }
  }

  /// purpose - have a challenge fighting jackals; learn about the importance of Grasses.
  void DetectJackalsVisible(ISteppable _) {
    var jackals = GameModel.main.currentFloor.bodies.OfType<Jackal>();
    if (!jackals.Any(j => j.isVisible)) {
      return;
    }
    GameModel.main.turnManager.OnStep -= DetectJackalsVisible;
    GameModel.main.player.ClearTasks();

    StartCoroutine(DelayedMessage());
    IEnumerator DelayedMessage() {
      yield return new WaitForSeconds(0.25f);
      Messages.Create("Jackals move fast. Use the Guardleaf!", 5);
    }
  }

  // private void DetectEnteredBerryBushRoom(ISteppable obj) {
  //   if (!tutFloor.berryBush.isVisible) {
  //     return;
  //   }
  //   GameModel.main.turnManager.OnStep -= DetectEnteredBerryBushRoom;
  //   GameModel.main.player.ClearTasks();

  //   Messages.Create("Harvest the Berry Bush!");
  // }

  // private void HandleFirstItemAdded(Item arg1, Entity arg2) {
  //   GameModel.main.player.inventory.OnItemAdded -= HandleFirstItemAdded;

  //   AnimateHorizontally(inventoryToggle, 900);
  //   AnimateHorizontally(inventoryContainer, 900);
  // }

  // private void HandleSeedPickup(Item item, Entity arg2) {
  //   if (item is ItemSeed) {
  //     GameModel.main.player.inventory.OnItemAdded -= HandleSeedPickup;
  //     Messages.Create("Plant the Seed!");
  //   }
  // }

  // private void HandleChangeWater(int delta) {
  //   AnimateHorizontally(waterIndicator, -900);
  //   GameModel.main.player.OnChangeWater -= HandleChangeWater;
  // }

  // private void DetectEnteredFinalRoom(ISteppable obj) {
  //   if (GameModel.main.player.pos.x < tutFloor.endRoom.min.x - 1) {
  //     return;
  //   }
  //   GameModel.main.turnManager.OnStep -= DetectEnteredFinalRoom;
  //   GameModel.main.player.ClearTasks();
  //   AnimateHorizontally(enemiesLeft, 900);

  //   Messages.Create("Bats attack other creatures!");
  // }

  private void HandleTutorialEnded() {
    var blackOverlay = HUDController.main.blackOverlay;
    if (Serializer.HasSave0()) {
      /// quit the tutorial.
      StartCoroutine(Transitions.GoToNewScene(this, blackOverlay, "Scenes/Intro"));
    } else {
      GameModel.GenerateNewGameAndSetMain();
      /// if there's no save, go straight to the real game
      GameModelController.main.StartCoroutine(Transitions.GoToNewScene(this, blackOverlay, "Scenes/Game"));
    }
  }

  void AnimateHorizontally(GameObject gameObject, float startX, float duration = 2) {
    gameObject.SetActive(true);
    /// temporarily disable d pad to prevent accidentally tapping the buttons while it's flying in
    var dpad = gameObject.GetComponent<DPadController>();
    if (dpad != null) {
      dpad.enabled = false;
    }
    var rt = gameObject.GetComponent<RectTransform>();
    var target = rt.anchoredPosition;
    StartCoroutine(Transitions.Animate(duration, (t) => {
      rt.anchoredPosition = Vector2.Lerp(new Vector2(startX, target.y), target, EasingFunctions.EaseOutCubic(0, 1, t));
      if (t == 1 && dpad != null) {
        dpad.enabled = true;
      }
    }));
  }
}
