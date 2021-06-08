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

  public static IEnumerator ZoomAndPanCamera(float targetSize, Nullable<Vector2> targetCenter = null, float duration = 1f, Func<float, float, float, float> easing = null) {
    var camera = Camera.main.GetComponent<Camera>();

    // disable camera state controllers
    var cameraFollowEntity = camera.GetComponent<CameraFollowEntity>();
    var cameraZoom = camera.GetComponent<CameraZoom>();
    cameraFollowEntity.enabled = false;
    cameraZoom.enabled = false;

    var startSize = camera.orthographicSize;
    var startPosition = camera.transform.position;
    var targetPosition3 = targetCenter == null ? startPosition : Util.withZ(targetCenter.Value, startPosition.z);
    yield return Animate(duration,
      callback: (t) => {
        camera.orthographicSize = EasingFunctions.Linear(startSize, targetSize, t);
        camera.transform.position = Vector3.Lerp(startPosition, targetPosition3, t);
      },
      post: () => {
        cameraFollowEntity.enabled = true;
        cameraZoom.enabled = true;
      },
      easing: easing ?? EasingFunctions.EaseOutCubic
    );
  }

  public static IEnumerator FadeAudio(AudioSource audioSource, float FadeTime, float targetVolume) {
    float startVolume = audioSource.volume;
    yield return Animate(FadeTime, (t) => {
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

  public static IEnumerator Animate(float duration, Action<float> callback, Action post = null, Func<float, float, float, float> easing = null) {
    if (easing == null) {
      easing = EasingFunctions.EaseInOutSine;
    }
    var start = Time.time;
    var tNorm = 0f;
    do {
      tNorm = (Time.time - start) / duration;
      var tEasing = easing(0, 1, tNorm);
      callback(tEasing);
      yield return new WaitForEndOfFrame();
    } while (tNorm < 1);
    callback(1);
    post?.Invoke();
  }
}
