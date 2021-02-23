using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Intro : MonoBehaviour {
  GameObject blackOverlay;
  public AudioClip playerMove;

  void Awake() {
    // unset current game.
    GameModel.main = null;
  }

  void Start() {
    blackOverlay = transform.Find("BlackOverlay").gameObject;
    if (!Serializer.HasSave()) {
      transform.Find("Continue").gameObject.SetActive(false);
    }
  }

  public void NewGame() {
    StartCoroutine(WalkPlayer());
    FadeOutButtonsAndMusic();
    #if UNITY_EDITOR
    // don't catch errors in dev so you get better stack trace
      GameModel.GenerateNewGameAndSetMain();
      GoToGameScene();
    #else
      try {
        //// TODO async this; need to stop using UnityEngine code.
        // await Task.Run(() => {
        GameModel.GenerateNewGameAndSetMain();
        // });
        GoToGameScene();
      } catch(Exception e) {
        /// TODO report to error server
        var popup = Popups.Create("Error Creating Game", e.Message, "", null);
        var controller = popup.GetComponent<PopupController>();
        controller.OnClose += () => SceneManager.LoadSceneAsync("Scenes/Intro");
      }
    #endif
  }

  public async void Continue() {
    StartCoroutine(WalkPlayer());
    FadeOutButtonsAndMusic();
    try {
      await Task.Run(() => {
        GameModel.main = Serializer.LoadFromFile();
      });
      GoToGameScene();
    } catch (Exception e) {
      var popup = Popups.Create("Could Not Load", e.Message, "", null);
      var controller = popup.GetComponent<PopupController>();
      controller.OnClose += () => SceneManager.LoadSceneAsync("Scenes/Intro");
    }
  }

  private IEnumerator WalkPlayer() {
    GameObject player = GameObject.Find("Player");
    Vector3 target = new Vector3(6.5f, -0.5f, 0);
    while (Vector3.Distance(player.transform.position, target) > Mathf.Epsilon) {
      Vector3 currentTarget = player.transform.position;
      currentTarget.x += 1;
      playerMove.PlayAtPoint(player.transform.position, 0.5f);
      while (Vector3.Distance(player.transform.position, currentTarget) > .005f) {
        player.transform.position = Vector3.Lerp(player.transform.position, currentTarget, 20 * Time.deltaTime);
        yield return new WaitForEndOfFrame();
      }
      player.transform.position = currentTarget;
      // yield return new WaitForSecondsRealtime(TurnManager.GAME_TIME_TO_SECONDS_WAIT_SCALE);
      yield return new WaitForSeconds(.2f);
    }
  }

  private void FadeOutButtonsAndMusic() {
    StartCoroutine(Transitions.FadeAudio(Camera.main.GetComponent<AudioSource>(), 1, 0));
    foreach (var button in GetComponentsInChildren<Button>()) {
      button.interactable = false;
    }
  }

  public void GoToGameScene() {
    StartCoroutine(Transitions.GoToNewScene(this, blackOverlay.GetComponent<Image>(), "Scenes/Game"));
  }
}
