using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeathScreen : MonoBehaviour {
  GameObject blackOverlay;

  void Start() {
    blackOverlay = transform.Find("BlackOverlay").gameObject;
    GameModel.main.player.OnDeath += HandlePlayerDeath;
    gameObject.SetActive(false);
  }

  private void HandlePlayerDeath() {
    gameObject.SetActive(true);
  }

  public void Restart() {
    StartCoroutine(LoadMainScene.TransitionToNewScene(blackOverlay.GetComponent<Image>(), "Scenes/Game"));
  }

  public void MainMenu() {
    StartCoroutine(LoadMainScene.TransitionToNewScene(blackOverlay.GetComponent<Image>(), "Scenes/Intro"));
  }
}
