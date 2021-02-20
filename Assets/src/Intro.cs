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
    StartCoroutine(EzraController.FadeOut(Camera.main.GetComponent<AudioSource>(), 1));
    foreach (var button in GetComponentsInChildren<Button>()) {
      button.interactable = false;
    }
  }

  private void GoToGameScene() {
    StartCoroutine(TransitionToNewScene(this, blackOverlay.GetComponent<Image>(), "Scenes/Game"));
  }

  public static IEnumerator TransitionToNewScene(MonoBehaviour b, Image overlay, string sceneToLoad) {
    SceneManager.LoadSceneAsync(sceneToLoad);
    yield return b.StartCoroutine(FadeTo(overlay));
  }

  public static IEnumerator FadeTo(Image overlay, float duration = 0.5f, Color? color = null) {
    if (color == null) {
      color = new Color(0, 0, 0, 1);
    }
    var start = Time.time;
    var t = 0f;
    var initColor = overlay.color;
    do {
      t = (Time.time - start) / duration;
      overlay.color = Color.Lerp(initColor, color.Value, t);
      yield return new WaitForEndOfFrame();
    } while (t < 1);
  }

  public static IEnumerator AnimateLinear(float duration, Action<float> callback) {
    var start = Time.time;
    var t = 0f;
    do {
      t = (Time.time - start) / duration;
      callback(t);
      yield return new WaitForEndOfFrame();
    } while (t < 1);
    callback(1);
  }
}
