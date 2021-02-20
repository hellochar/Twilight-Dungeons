using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialFloorController : FloorController {
  private TutorialFloor tutFloor => (TutorialFloor) floor;
  GameObject dPad, hpBar, waterIndicator, inventoryToggle, inventoryContainer, sidePanel, depth;
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
    waterIndicator = GameObject.Find("Water Indicator");
    inventoryToggle = GameObject.Find("Inventory Toggle");
    inventoryContainer = GameObject.Find("Inventory Container");
    sidePanel = GameObject.Find("Side Panel");
    depth = GameObject.Find("Depth");
    allUI = new List<GameObject>() { dPad, hpBar, waterIndicator, inventoryToggle, inventoryContainer, sidePanel, depth };

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
        case FloorMessage.TutorialEnd:
          TutorialEnd();
          break;
      }
    };
  }

  void OnDestroy() {
    Settings.Set(Settings.LoadOrGetDefaultSettings());
  }

  private void BlobRoomEntered() {
    /// show dpad, show HP, and explain "tap hold"
    dPad.SetActive(true);
    hpBar.SetActive(true);
    Messages.Create("Tap-and-hold on the Blob to learn about it.", 5);
  }

  private void JackalRoomEntered() {
    sidePanel.SetActive(true);
    Messages.Create("Jackals run fast! The Guardleaf will protect you.", 5);
  }

  private void BerryBushRoomEntered() {
    inventoryToggle.SetActive(true);
    inventoryContainer.SetActive(true);
    waterIndicator.SetActive(true);
    Messages.Create("You found a Berry Bush! Tap it to see what you can do with it.");
    // var berryBush = tutFloor.berryBush;
    // once they tap it
    // "You may harvest items from this Berry Bush! Tap the items to learn about them, then tap harvest."
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
