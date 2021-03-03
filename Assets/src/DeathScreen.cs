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

  public void NewGame() {
    GameModel.GenerateNewGameAndSetMain();
    StartCoroutine(Transitions.GoToNewScene(this, blackOverlay.GetComponent<Image>(), "Scenes/Game"));
  }

  public void MainMenu() {
    StartCoroutine(Transitions.GoToNewScene(this, blackOverlay.GetComponent<Image>(), "Scenes/Intro"));
  }
}
