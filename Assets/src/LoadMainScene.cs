using System;
using System.Collections;
using System.Collections.Generic;
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
    StartGame();
  }

  public void Continue() {
    GameModel.shouldLoad = true;
    StartGame();
  }

  private void StartGame() {
    StartCoroutine(TransitionToNewScene(this, blackOverlay.GetComponent<Image>(), "Scenes/Game"));
  }

  public static IEnumerator TransitionToNewScene(MonoBehaviour b, Image overlay, string sceneToLoad) {
    yield return b.StartCoroutine(FadeToBlack(overlay));
    SceneManager.LoadSceneAsync(sceneToLoad);
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
