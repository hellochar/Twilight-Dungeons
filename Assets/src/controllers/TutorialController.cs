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
    // hide most of the UI by default
    var HUD = HUDController.main;
    HUD.hpBar?.SetActive(false);
    HUD.statuses?.SetActive(false);
    HUD.waterIndicator?.SetActive(false);
    HUD.inventoryToggle?.SetActive(false);
    HUD.inventoryContainer?.SetActive(false);
    // HUD.depth?.SetActive(false);
    HUD.enemiesLeft?.SetActive(false);
    // HUD.waitButton?.SetActive(false);
    HUD.settings?.SetActive(false);
    HUD.damageFlash.SetActive(false);

    // AddHighlights();

    Player player = GameModel.main.player;

    if (GameModel.main.currentFloor is TutorialFloor tf && tf.name == "T_Room1") {
      _ = Messages.CreateDelayed("Tap to move.", 1, 5);
    }
    player.inventory.OnItemAdded += HandleFirstItemAdded;             // redberries
    player.nonserializedModifiers.Add(this);                          // getting a status and healing
    GameModel.main.turnManager.OnStep += DetectJackalsVisible;        // jackal room
    GameModel.main.turnManager.OnStep += DetectGuardleafVisible;      // guardleaf room



    TutorialFloor.OnTutorialEnded += HandleTutorialEnded;                  // end!
  }

  public void HandleHeal(int amount) {
    if (!HUDController.main.hpBar.activeSelf) {
      Transitions.AnimateUIHorizontally(HUDController.main.hpBar, 900);
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
      Transitions.AnimateUIHorizontally(HUDController.main.statuses, 900);
    }
  }

  void DetectJackalsVisible(ISteppable s) {
    var jackals = GameModel.main.currentFloor.bodies.OfType<Jackal>();
    if (!jackals.Any()) {
      return;
    }
    GameModel.main.turnManager.OnStep -= DetectJackalsVisible;

    _ = Messages.CreateDelayed("Jackals move fast but get scared!", 1, 5);
  }

  void DetectGuardleafVisible(ISteppable s) {
    var guardleaf = GameModel.main.currentFloor.grasses.OfType<Guardleaf>();
    if (!guardleaf.Any()) {
      return;
    }
    GameModel.main.turnManager.OnStep -= DetectGuardleafVisible;

    _ = Messages.CreateDelayed("Protect yourself in the Guardleaf!", 1, 5);
  }

  private void HandleFirstItemAdded(Item arg1, Entity arg2) {
    GameModel.main.player.inventory.OnItemAdded -= HandleFirstItemAdded;

    Transitions.AnimateUIHorizontally(HUDController.main.inventoryToggle, 900);
    Transitions.AnimateUIHorizontally(HUDController.main.inventoryContainer, 900);
  }

  private void HandleTutorialEnded() {
    var blackOverlay = HUDController.main.blackOverlay;
    if (Serializer.HasSave0()) {
      /// quit the tutorial.
      GameModelController.main.StartCoroutine(Transitions.GoToNewScene(this, blackOverlay, "Scenes/Intro"));
    } else {
      GameModel.GenerateNewGameAndSetMain();
      /// if there's no save, go straight to the real game
      GameModelController.main.StartCoroutine(Transitions.GoToNewScene(this, blackOverlay, "Scenes/Game"));
    }
  }
}
