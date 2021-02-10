using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadMainScene : MonoBehaviour {
  GameObject blackOverlay;

  void Start() {
    blackOverlay = transform.Find("BlackOverlay").gameObject;
    if (!Serializer.HasSave()) {
      transform.Find("Continue").gameObject.SetActive(false);
    }
  }

  public void NewGame() {
    StartCoroutine(WalkPlayer());
    //// TODO async this; need to stop using UnityEngine code.
    GameModel.GenerateNewGameAndSetMain();
    GoToGameScene();
  }

  public async void Continue() {
    StartCoroutine(WalkPlayer());
    try {
      await Task.Run(() => {
        GameModel.main = Serializer.LoadFromFile();
      });
      GoToGameScene();
    } catch (Exception e) {
      var popup = Popups.Create("Could Not Load", e.Message, "", null);
      // var controller = popup.GetComponent<PopupController>();
      // controller.OnClose += () => SceneManager.LoadSceneAsync("Scenes/Intro");
    }
  }

  private IEnumerator WalkPlayer() {
    GameObject player = GameObject.Find("Player");
    Vector3 target = new Vector3(6.5f, -0.5f, 0);
    while (Vector3.Distance(player.transform.position, target) > Mathf.Epsilon) {
      Vector3 currentTarget = player.transform.position;
      currentTarget.x += 1;
      while (Vector3.Distance(player.transform.position, currentTarget) > .005f) {
        player.transform.position = Vector3.Lerp(player.transform.position, currentTarget, 20 * Time.deltaTime);
        yield return new WaitForEndOfFrame();
      }
      player.transform.position = currentTarget;
      // yield return new WaitForSecondsRealtime(TurnManager.GAME_TIME_TO_SECONDS_WAIT_SCALE);
      yield return new WaitForSeconds(.2f);
    }
  }

  private void GoToGameScene() {
    foreach (var button in GetComponentsInChildren<Button>()) {
      button.interactable = false;
    }
    StartCoroutine(TransitionToNewScene(this, blackOverlay.GetComponent<Image>(), "Scenes/Game"));
  }

  public static IEnumerator TransitionToNewScene(MonoBehaviour b, Image overlay, string sceneToLoad) {
    SceneManager.LoadSceneAsync(sceneToLoad);
    yield return b.StartCoroutine(FadeToBlack(overlay));
  }

  public static IEnumerator FadeToBlack(Image overlay, float duration = 0.5f) {
    var start = Time.time;
    var t = 0f;
    do {
      t = (Time.time - start) / duration;
      overlay.color = Color.Lerp(new Color(0, 0, 0, 0), new Color(0, 0, 0, 1), t);
      yield return new WaitForEndOfFrame();
    } while (t < 1);
  }
}
