using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeathScreen : MonoBehaviour, IDeathHandler {
  GameObject blackOverlay;

  void Start() {
    blackOverlay = transform.parent.Find("BlackOverlay").gameObject;
    GameModel.main.player.nonserializedModifiers.Add(this);
    gameObject.SetActive(false);
  }

  public void HandleDeath(Entity source) {
    gameObject.SetActive(true);
  }

  public void Restart() {
    GameModel.GenerateNewGameAndSetMain();
    StartCoroutine(LoadMainScene.TransitionToNewScene(this, blackOverlay.GetComponent<Image>(), "Scenes/Game"));
  }

  public void MainMenu() {
    StartCoroutine(LoadMainScene.TransitionToNewScene(this, blackOverlay.GetComponent<Image>(), "Scenes/Intro"));
  }
}
