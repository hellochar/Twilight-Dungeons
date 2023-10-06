using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitButtonController : MonoBehaviour {
  public void HandleWaitPressed() {
    if (InteractionController.isInputAllowed) {
      GameModel.main.player.SetTasks(new WaitTask(GameModel.main.player, 1));
    }
  }
}
