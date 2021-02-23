using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Transitions {
  public static IEnumerator GoToNewScene(MonoBehaviour b, Image overlay, string sceneToLoad) {
    SceneManager.LoadSceneAsync(sceneToLoad);
    yield return b.StartCoroutine(Transitions.FadeTo(overlay));
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

  public static IEnumerator FadeAudio(AudioSource audioSource, float FadeTime, float targetVolume) {
    float startVolume = audioSource.volume;
    yield return AnimateLinear(FadeTime, (t) => {
      audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
    });
  }

  public static IEnumerator SpriteSwap(SpriteRenderer sr, float duration = 1f, params Sprite[] sprites) {
    // everything but the last one
    for(var i = 0; i < sprites.Length - 1; i++) {
      sr.sprite = sprites[i];
      yield return new WaitForSeconds(duration / (sprites.Length - 1));
    }
    // go to the last one at the very end
    sr.sprite = sprites[sprites.Length - 1];
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
