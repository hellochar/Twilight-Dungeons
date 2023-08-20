using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitButtonController : MonoBehaviour {
  // Start is called before the first frame update
  void Start() {
    Settings.OnChanged += MatchSettings;
    MatchSettings();
  }

  void OnDestroy() {
    Settings.OnChanged -= MatchSettings;
  }

  private void MatchSettings() {
    gameObject?.SetActive(!Settings.main.useDPad);
  }

  public void HandleWaitPressed() {
    if (InteractionController.isInputAllowed) {
      GameModel.main.player.SetTasks(new WaitTask(GameModel.main.player, 1));
    }
  }
}
