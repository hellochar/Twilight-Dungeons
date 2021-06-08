using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialFloorController : FloorController, IStatusAddedHandler {
  private TutorialFloor tutFloor => (TutorialFloor) floor;
  GameObject hpBar, waterIndicator, inventoryToggle, inventoryContainer, gear, statuses, depth, sidePanel, enemiesLeft;
  List<GameObject> allUI;

  // Start is called before the first frame update
  public override void Start() {
    base.Start();

    /// force default settings because touch to move might be turned off and user could get stuck
    /// must do this *before* hiding UI since d-pad hiding is implemented using SetActive as well
    Settings.Set(Settings.Default(), false);

    // hide all the UI by default
    hpBar = GameObject.Find("Hearts");
    statuses = GameObject.Find("Statuses");
    waterIndicator = GameObject.Find("Water Indicator");
    inventoryToggle = GameObject.Find("Inventory Toggle");
    inventoryContainer = GameObject.Find("Inventory Container");
    depth = GameObject.Find("Depth");
    gear = GameObject.Find("Gear");
    sidePanel = GameObject.Find("Side Panel");
    enemiesLeft = GameObject.Find("Enemies Left");
    allUI = new List<GameObject>() { hpBar, statuses, waterIndicator, inventoryToggle, inventoryContainer, depth, gear, sidePanel, enemiesLeft };

    foreach (var ui in allUI) {
      ui.SetActive(false);
    }

    AddHighlights();

    Player player = GameModel.main.player;

    // the order of these statements follows the order in which the player will hit them in the tutorial
    StartTutorial();
    GameModel.main.turnManager.OnStep += DetectBlobVisible;           // blob room
    player.nonserializedModifiers.Add(this);                          // guardleaf status
    GameModel.main.turnManager.OnStep += DetectJackalsVisible;        // jackal room
    GameModel.main.turnManager.OnStep += DetectEnteredBerryBushRoom;  // berry bush
    player.inventory.OnItemAdded += HandleFirstItemAdded;             // after harvesting and picking up the first item
    player.inventory.OnItemAdded += HandleSeedPickup;       // after picking up all 4 items
    player.OnChangeWater += HandleChangeWater;                        // after getting water
    GameModel.main.turnManager.OnStep += DetectEnteredFinalRoom;      // final room
    tutFloor.OnTutorialEnded += HandleTutorialEnded;                  // end!
  }

  void AddHighlights() {
    PrefabCache.Effects.Instantiate("Highlight", GameObjectFor(tutFloor.blob).transform);
    PrefabCache.Effects.Instantiate("Highlight", GameObjectFor(tutFloor.guardleaf).transform);
    PrefabCache.Effects.Instantiate("Highlight", GameObjectFor(tutFloor.jackals[0]).transform);
    PrefabCache.Effects.Instantiate("Highlight", GameObjectFor(tutFloor.berryBush).transform);
    PrefabCache.Effects.Instantiate("Highlight", GameObjectFor(tutFloor.astoria).transform);
    PrefabCache.Effects.Instantiate("Highlight", GameObjectFor(tutFloor.bat).transform);
  }

  void StartTutorial() {
    StartCoroutine(DelayedMessage());
    IEnumerator DelayedMessage() {
      yield return new WaitForSeconds(1f);
      Messages.Create("Use D-Pad to move.", 5);
    }
  }

  /// show HP, and explain "tap hold"
  void DetectBlobVisible(ISteppable _) {
    if (!tutFloor.blob.isVisible) {
      return;
    }

    GameModel.main.turnManager.OnStep -= DetectBlobVisible;
    GameModel.main.player.ClearTasks();

    // 900px is Canvas's canvas scalar reference resolution
    GameModel.main.player.AddTimedEvent(2, () => AnimateHorizontally(hpBar, 900));
    StartCoroutine(DelayedMessage());
    IEnumerator DelayedMessage() {
      yield return new WaitForSeconds(0.25f);
      Messages.Create("Tap on things to learn about them.", 5);
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
      Messages.Create("Jackals move fast. Use the Guardleaf!", 5);
    }
  }

  private void DetectEnteredBerryBushRoom(ISteppable obj) {
    if (!tutFloor.berryBush.isVisible) {
      return;
    }
    GameModel.main.turnManager.OnStep -= DetectEnteredBerryBushRoom;
    GameModel.main.player.ClearTasks();

    Messages.Create("Harvest the Berry Bush!");
  }

  private void HandleFirstItemAdded(Item arg1, Entity arg2) {
    GameModel.main.player.inventory.OnItemAdded -= HandleFirstItemAdded;

    AnimateHorizontally(inventoryToggle, 900);
    AnimateHorizontally(inventoryContainer, 900);
  }

  private void HandleSeedPickup(Item item, Entity arg2) {
    if (item is ItemSeed) {
      GameModel.main.player.inventory.OnItemAdded -= HandleSeedPickup;
      Messages.Create("Plant the Seed!");
    }
  }

  private void HandleChangeWater(int delta) {
    AnimateHorizontally(waterIndicator, -900);
    GameModel.main.player.OnChangeWater -= HandleChangeWater;
  }

  private void DetectEnteredFinalRoom(ISteppable obj) {
    if (GameModel.main.player.pos.x < tutFloor.endRoom.min.x - 1) {
      return;
    }
    GameModel.main.turnManager.OnStep -= DetectEnteredFinalRoom;
    GameModel.main.player.ClearTasks();
    AnimateHorizontally(enemiesLeft, 900);

    Messages.Create("Bats attack other creatures!");
  }

  private void HandleTutorialEnded() {
    Settings.Set(Settings.LoadOrGetDefaultSettings());
    var blackOverlay = GameObject.Find("BlackOverlay");
    if (Serializer.HasSave()) {
      /// quit the tutorial.
      StartCoroutine(Transitions.GoToNewScene(this, blackOverlay.GetComponent<Image>(), "Scenes/Intro"));
    } else {
      GameModel.GenerateNewGameAndSetMain();
      /// if there's no save, go straight to the real game
      StartCoroutine(Transitions.GoToNewScene(this, blackOverlay.GetComponent<Image>(), "Scenes/Game"));
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
