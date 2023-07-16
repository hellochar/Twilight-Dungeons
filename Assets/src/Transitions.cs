using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Transitions {
  public static IEnumerator GoToNewScene(MonoBehaviour b, Image overlay, string sceneToLoad) {
    SceneManager.LoadSceneAsync(sceneToLoad);
    if (overlay != null) {
      yield return b.StartCoroutine(Transitions.FadeImage(overlay, Color.clear, Color.black));
    }
  }

  // by default, go from its current color to the target color
  public static IEnumerator FadeImage(Image overlay, Color start, Color end, float duration = 0.5f) {
    var t0 = Time.time;
    var t = 0f;
    do {
      t = (Time.time - t0) / duration;
      overlay.color = Color.Lerp(start, end, t);
      yield return new WaitForEndOfFrame();
    } while (t < 1);
  }

  public static IEnumerator ZoomAndPanCamera(float targetSize, Nullable<Vector2> targetCenter = null, float duration = 1f, Func<float, float, float, float> easing = null) {
    // TODO integrate this with CameraController
    var cameraController = CameraController.main;
    var camera = cameraController.camera;

    // disable camera state controllers
    cameraController.enabled = false;

    var startSize = camera.orthographicSize;
    var startPosition = camera.transform.position;
    var targetPosition3 = targetCenter == null ? startPosition : Util.withZ(targetCenter.Value, startPosition.z);
    yield return Animate(duration,
      callback: (t) => {
        camera.orthographicSize = EasingFunctions.Linear(startSize, targetSize, t);
        camera.transform.position = Vector3.Lerp(startPosition, targetPosition3, t);
      },
      post: () => {
        cameraController.enabled = true;
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

  public static void AnimateUIHorizontally(GameObject gameObject, float startX, float duration = 2) {
    gameObject.SetActive(true);
    /// temporarily disable d pad to prevent accidentally tapping the buttons while it's flying in
    var dpad = gameObject.GetComponent<DPadController>();
    if (dpad != null) {
      dpad.enabled = false;
    }
    var rt = gameObject.GetComponent<RectTransform>();
    var target = rt.anchoredPosition;
    GameModelController.main.StartCoroutine(Animate(duration, (t) => {
      rt.anchoredPosition = Vector2.Lerp(new Vector2(startX, target.y), target, EasingFunctions.EaseOutCubic(0, 1, t));
      if (t == 1 && dpad != null) {
        dpad.enabled = true;
      }
    }));
  }
}
