using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverSceneController : MonoBehaviour {
  public GameObject button;
  public TMPro.TMP_Text heading;
  public Image blackOverlay;

  void Start() {
    if (GameModel.main.stats.won) {
      // scene is set up for this by default; no change
    } else {
      heading.text = $"You perished to {GameModel.main.stats.killedBy}...";
      button.GetComponent<Image>().color = Color.white;
      var text = button.GetComponentInChildren<TMPro.TMP_Text>();
      text.text = "Restart";
      text.color = Color.black;
    }
  }

  public void HandleButtonPressed() {
    if (GameModel.main.stats.won) {
      TheEnd();
    } else {
      Restart();
    }
  }

  private void Restart() {
    GameModel.GenerateNewGameAndSetMain();
    StartCoroutine(Transitions.GoToNewScene(this, blackOverlay, "Scenes/Game"));
  }

  private void TheEnd() {
    StartCoroutine(Transitions.GoToNewScene(this, blackOverlay, "Scenes/Intro"));
  }
}
