using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialFloorController : FloorController, IStatusAddedHandler {
  private TutorialFloor tutFloor => (TutorialFloor) floor;
  GameObject dPad, hpBar, waterIndicator, inventoryToggle, inventoryContainer, gear, statuses, sidePanel, depth;
  List<GameObject> allUI;

  // Start is called before the first frame update
  public override void Start() {
    base.Start();

    /// force default settings because touch to move might be turned off and user could get stuck
    /// must do this *before* hiding UI since d-pad hiding is implemented using SetActive as well
    Settings.Set(Settings.Default(), false);

    // hide all the UI by default
    dPad = GameObject.Find("DPad");
    hpBar = GameObject.Find("Hearts");
    statuses = GameObject.Find("Statuses");
    waterIndicator = GameObject.Find("Water Indicator");
    inventoryToggle = GameObject.Find("Inventory Toggle");
    inventoryContainer = GameObject.Find("Inventory Container");
    sidePanel = GameObject.Find("Side Panel");
    depth = GameObject.Find("Depth");
    gear = GameObject.Find("Gear");
    allUI = new List<GameObject>() { dPad, hpBar, statuses, waterIndicator, inventoryToggle, inventoryContainer, sidePanel, depth, gear };

    foreach (var ui in allUI) {
      ui.SetActive(false);
    }

    Player player = GameModel.main.player;

    // the order of these statements follows the order in which the player will hit them in the tutorial
    GameModel.main.turnManager.OnStep += DetectBlobVisible;           // blob room
    player.nonserializedModifiers.Add(this);                          // guardleaf status
    GameModel.main.turnManager.OnStep += DetectJackalsVisible;        // jackal room
    GameModel.main.turnManager.OnStep += DetectEnteredBerryBushRoom;  // berry bush
    player.inventory.OnItemAdded += HandleFirstItemAdded;             // after harvesting and picking up the first item
    player.inventory.OnItemAdded += HandleAllFourItemsPickedUp;       // after picking up all 4 items
    player.OnGetWater += HandleGetWater;                              // after getting water
    GameModel.main.turnManager.OnStep += DetectEnteredFinalRoom;      // final room
    tutFloor.OnTutorialEnded += HandleTutorialEnded;                  // end!
  }

  /// show dpad, show HP, and explain "tap hold"
  void DetectBlobVisible(ISteppable _) {
    if (!tutFloor.blob.isVisible) {
      return;
    }

    GameModel.main.turnManager.OnStep -= DetectBlobVisible;
    GameModel.main.player.ClearTasks();

    // 900px is Canvas's canvas scalar reference resolution
    GameModel.main.player.AddTimedEvent(2, () => AnimateHorizontally(hpBar, 900));
    // Add 100px buffer because the dPad's anchoredPosition is relative to the center of the pad, which means
    // there's left-bleed
    GameModel.main.player.AddTimedEvent(9, () => AnimateHorizontally(dPad, 1000));
    StartCoroutine(DelayedMessage());
    IEnumerator DelayedMessage() {
      yield return new WaitForSeconds(0.25f);
      Messages.Create("Tap-and-hold on the Blob to learn about it.", 5);
    }
  }

  public void HandleStatusAdded(Status status) {
    if (!statuses.activeSelf) {
      AnimateHorizontally(statuses, 900);
    }
  }

  /// purpose - have a challenge fighting jackals; learn about the importance of Grasses.
  void DetectJackalsVisible(ISteppable _) {
    if (!tutFloor.jackals.Any(j => j.isVisible)) {
      return;
    }
    GameModel.main.turnManager.OnStep -= DetectJackalsVisible;
    GameModel.main.player.ClearTasks();

    StartCoroutine(DelayedMessage());
    IEnumerator DelayedMessage() {
      yield return new WaitForSeconds(0.25f);
      Messages.Create("Jackals move fast! Guardleaf will protect you.", 5);
    }
  }

  private void DetectEnteredBerryBushRoom(ISteppable obj) {
    if (GameModel.main.player.pos.x < 30) {
      return;
    }
    GameModel.main.turnManager.OnStep -= DetectEnteredBerryBushRoom;
    GameModel.main.player.ClearTasks();

    Messages.Create("Tap the Berry Bush to Harvest it!");
  }

  private void HandleFirstItemAdded(Item arg1, Entity arg2) {
    GameModel.main.player.inventory.OnItemAdded -= HandleFirstItemAdded;

    AnimateHorizontally(inventoryToggle, 900);
    AnimateHorizontally(inventoryContainer, 900);
  }

  int itemsPickedUp = 0;
  private void HandleAllFourItemsPickedUp(Item arg1, Entity arg2) {
    if (++itemsPickedUp < 4) {
      return;
    }
    GameModel.main.player.inventory.OnItemAdded -= HandleAllFourItemsPickedUp;

    Messages.Create("Plant the seeds to grow more Berry Bushes!");
  }

  private void HandleGetWater() {
    AnimateHorizontally(waterIndicator, -900);
    GameModel.main.player.OnGetWater -= HandleGetWater;
  }

  private void DetectEnteredFinalRoom(ISteppable obj) {
    if (GameModel.main.player.pos.x < 40) {
      return;
    }
    GameModel.main.turnManager.OnStep -= DetectEnteredFinalRoom;
    GameModel.main.player.ClearTasks();

    // come in from left
    AnimateHorizontally(sidePanel, -900);
  }

  private void HandleTutorialEnded() {
    Settings.Set(Settings.LoadOrGetDefaultSettings());
    var blackOverlay = GameObject.Find("BlackOverlay");
    if (Serializer.HasSave()) {
      /// quit the tutorial.
      StartCoroutine(Intro.TransitionToNewScene(this, blackOverlay.GetComponent<Image>(), "Scenes/Intro"));
    } else {
      GameModel.GenerateNewGameAndSetMain();
      /// if there's no save, go straight to the real game
      StartCoroutine(Intro.TransitionToNewScene(this, blackOverlay.GetComponent<Image>(), "Scenes/Game"));
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
    StartCoroutine(Intro.AnimateLinear(duration, (t) => {
      rt.anchoredPosition = Vector2.Lerp(new Vector2(startX, target.y), target, EasingFunctions.EaseOutCubic(0, 1, t));
      if (t == 1 && dpad != null) {
        dpad.enabled = true;
      }
    }));
  }
}
