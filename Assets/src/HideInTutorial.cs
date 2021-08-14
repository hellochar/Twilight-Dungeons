using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideInTutorial : MonoBehaviour {
  void Start() {
    if (GameModel.main.home is TutorialFloor && !PrologueController.HasFinishedTutorial()) {
      // We don't have to re-enable because the Scene will reset
      gameObject.SetActive(false);
    }
  }
}
