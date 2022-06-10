using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitButtonController : MonoBehaviour {
  // Start is called before the first frame update
  void Start() {
    Settings.OnChanged += MatchSettings;
    MatchSettings();
  }

  void OnDestroyed() {
    Settings.OnChanged -= MatchSettings;
  }

  private void MatchSettings() {
    gameObject?.SetActive(!Settings.main.useDPad);
  }

  public void HandleWaitPressed() {
    var interactionController = GameModelController.main.CurrentFloorController.GetComponent<InteractionController>();
    var pos = GameModel.main.player.pos;
    interactionController.Interact(pos, null);
  }
}
