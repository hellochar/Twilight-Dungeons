using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Fade all renderers in this hierarchy over 1 second and then Destroy() the gameObject it's attached to
public class FadeThenDestroy : MonoBehaviour {
  static float FADE_TIME = 0.5f;
  private SpriteRenderer[] renderers;
  private Color[] colors;
  private Vector3[] scales;
  float startTime;
  void Start() {
    renderers = GetComponentsInChildren<SpriteRenderer>();
    colors = new Color[renderers.Length];
    scales = new Vector3[renderers.Length];
    for (int i = 0; i < renderers.Length; i++) {
      colors[i] = renderers[i].color;
      scales[i] = renderers[i].transform.localScale;
    }
    startTime = Time.time;
  }

  void Update() {
    var t = (Time.time - startTime) / FADE_TIME;
    if (t >= 1) {
      Destroy(this.gameObject);
      return;
    }
    for (int i = 0; i < renderers.Length; i++) {
      if (renderers[i] != null) {
        var originalColor = colors[i];
        var newColor = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a * (1 - t));
        var newScale = scales[i] * (1 + 0.2f * t);
        renderers[i].color = newColor;
        renderers[i].transform.localScale = newScale;
      }
    }
  }
}
