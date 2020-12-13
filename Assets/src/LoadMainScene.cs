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
  }

  public void StartGame() {
    StartCoroutine(FadeBlackOverlay());
  }

  private IEnumerator FadeBlackOverlay() {
    var start = Time.time;
    var t = 0f;
    do {
      t = (Time.time - start) / 0.5f;
      blackOverlay.GetComponent<Image>().color = Color.Lerp(new Color(0, 0, 0, 0), new Color(0, 0, 0, 1), t);
      yield return new WaitForEndOfFrame();
    } while (t < 1);
    SceneManager.LoadSceneAsync("Scenes/Game");
  }
}
