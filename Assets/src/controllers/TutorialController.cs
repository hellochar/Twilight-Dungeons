﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialController : MonoBehaviour, IStatusAddedHandler, IHealHandler {
  public static bool HasFinishedTutorial() {
    return PlayerPrefs.HasKey("hasSeenPrologue");
  }

  internal static void SetFinishedTutorial() {
    PlayerPrefs.SetInt("hasSeenPrologue", 1);
  }

  // Start is called before the first frame update
  public void Start() {
    var model = GameModel.main.tutorialModel;
    if (model == null) {
      Destroy(this);
      throw new Exception("TutorialController requires a TutorialModel");
    }

    foreach (var element in model.HUD.Values) {
      HUDController.main.GetHUDGameObject(element.name).SetActive(element.active);
    }

    Player player = GameModel.main.player;

    if (GameModel.main.currentFloor is TutorialFloor tf && tf.name == "T_Room1") {
      _ = Messages.CreateDelayed("Tap to move.", 1, 5);
    }
    player.inventory.OnItemAdded += HandleFirstItemAdded;             // stick in T_Healing
    player.nonserializedModifiers.Add(this);                          // HandleStatusAdded (T_Guardleaf) and HandleHeal (T_Healing)
    GameModel.main.turnManager.OnStep += DetectJackalsVisible;        // starting T_Jackals
    GameModel.main.turnManager.OnStep += DetectGuardleafVisible;      // starting T_Guardleaf

    TutorialFloor.OnTutorialEnded += HandleTutorialEnded;                  // end!
  }

  public void HandleHeal(int amount) {
    ShowHUDElement(HUDController.main.hpBar);
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
    ShowHUDElement(HUDController.main.statuses);
  }

  void DetectJackalsVisible(ISteppable s) {
    var jackals = GameModel.main.currentFloor.bodies.OfType<Jackal>();
    if (!jackals.Any()) {
      return;
    }
    GameModel.main.turnManager.OnStep -= DetectJackalsVisible;

    _ = Messages.CreateDelayed("Equip your stick!", 1, 5);
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

    ShowHUDElement(HUDController.main.inventoryToggle);
    ShowHUDElement(HUDController.main.inventoryContainer);
  }

  private void HandleTutorialEnded() {
    SetFinishedTutorial();
    var blackOverlay = HUDController.main.blackOverlay;
    if (Serializer.HasSave0()) {
      /// quit the tutorial.
      GameModelController.main.StartCoroutine(Transitions.GoToNewScene(GameModelController.main, blackOverlay, "Scenes/Intro"));
    } else {
      GameModel.GenerateNewGameAndSetMain();
      /// if there's no save, go straight to the real game
      GameModelController.main.StartCoroutine(Transitions.GoToNewScene(GameModelController.main, blackOverlay, "Scenes/Game"));
    }
  }

  private void ShowHUDElement(GameObject hudElement, int startX = 900) {
    if (!hudElement.activeSelf) {
      Transitions.AnimateUIHorizontally(hudElement, startX);
      // GameModel.main.player.AddTimedEvent(2, () => AnimateHorizontally(hpBar, 900));
      GameModel.main.tutorialModel.FindHUDElement(hudElement).active = true;
    }
  }
}
