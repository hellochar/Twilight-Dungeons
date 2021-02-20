using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialFloorController : FloorController, IStatusAddedHandler {
  private TutorialFloor tutFloor => (TutorialFloor) floor;
  GameObject dPad, hpBar, waterIndicator, inventoryToggle, inventoryContainer, statuses, sidePanel, depth;
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
    allUI = new List<GameObject>() { dPad, hpBar, statuses, waterIndicator, inventoryToggle, inventoryContainer, sidePanel, depth };

    foreach (var ui in allUI) {
      ui.SetActive(false);
    }

    tutFloor.OnMessage += (message) => {
      switch (message) {
        case FloorMessage.BlobRoomEntered:
          BlobRoomEntered();
          break;
        case FloorMessage.JackalRoomEntered:
          JackalRoomEntered();
          break;
        case FloorMessage.BerryBushRoomEntered:
          BerryBushRoomEntered();
          break;
        case FloorMessage.FinalRoomEntered:
          FinalRoomEntered();
          break;
        case FloorMessage.TutorialEnd:
          TutorialEnd();
          break;
      }
    };

    Player player = GameModel.main.player;
    player.nonserializedModifiers.Add(this);
    player.inventory.OnItemAdded += HandleItemAdded;
    player.OnGetWater += HandleGetWater;
  }

  private void FinalRoomEntered() {
    // come in from left
    AnimateHorizontally(sidePanel, -900);
    StartCoroutine(DelayedMessage());
    IEnumerator DelayedMessage() {
      yield return new WaitForSeconds(3f);
      Messages.Create("Some enemies attack each other!", 3);
    }
  }

  void OnDestroy() {
    Settings.Set(Settings.LoadOrGetDefaultSettings());
  }

  /// show dpad, show HP, and explain "tap hold"
  private void BlobRoomEntered() {
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

  public void HandleStatusAdded(Status status) {
    if (!statuses.activeSelf) {
      AnimateHorizontally(statuses, 900);
    }
  }

  private void HandleItemAdded(Item arg1, Entity arg2) {
    AnimateHorizontally(inventoryToggle, 900);
    AnimateHorizontally(inventoryContainer, 900);
    GameModel.main.player.inventory.OnItemAdded -= HandleItemAdded;
  }

  private void HandleGetWater() {
    AnimateHorizontally(waterIndicator, -900);
    GameModel.main.player.OnGetWater -= HandleGetWater;
  }


  /// purpose - have a challenge fighting jackals; learn about the importance of Grasses.
  private void JackalRoomEntered() {
    StartCoroutine(DelayedMessage());
    IEnumerator DelayedMessage() {
      yield return new WaitForSeconds(0.25f);
      Messages.Create("Jackals move fast! Guardleaf will protect you.", 5);
    }
  }

  private void BerryBushRoomEntered() {
    // waterIndicator.SetActive(true);
    Messages.Create("Tap the Berry Bush to Harvest it!");
    // once they harvest
    // "Great! You now have items to help you survive, as well as Berry Bush seeds to re-plant."
    // "Plant your berry bush seeds now. You'll need one water per seed. Collect water by tapping on it."
  }

  private void TutorialEnd() {
    /// quit the scenario and go back to the main screen
    var blackOverlay = GameObject.Find("BlackOverlay");
    StartCoroutine(Intro.TransitionToNewScene(this, blackOverlay.GetComponent<Image>(), "Scenes/Intro"));
  }
}
