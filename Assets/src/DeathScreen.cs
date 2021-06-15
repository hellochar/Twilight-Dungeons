using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeathScreen : MonoBehaviour, IDeathHandler {
  public Image blackOverlay;

  void Start() {
    GameModel.main.player.nonserializedModifiers.Add(this);
    gameObject.SetActive(false);
  }

  public void HandleDeath(Entity source) {
    gameObject.SetActive(true);
  }

  public void NewGame() {
    GameModel.GenerateNewGameAndSetMain();
    StartCoroutine(Transitions.GoToNewScene(this, blackOverlay, "Scenes/Game"));
  }

  public void MainMenu() {
    StartCoroutine(Transitions.GoToNewScene(this, blackOverlay, "Scenes/Intro"));
  }
}
