using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Fade all renderers in this hierarchy over 1 second and then Destroy() the gameObject it's attached to
public class FadeThenDestroy : MonoBehaviour {
  static float FADE_TIME = 1.0f;
  private SpriteRenderer[] renderers;
  private Color[] originalColors;
  float startTime;
  void Start() {
    renderers = GetComponentsInChildren<SpriteRenderer>();
    originalColors = new Color[this.renderers.Length];
    for (int i = 0; i < renderers.Length; i++) {
      originalColors[i] = renderers[i].color;
    }
    startTime = Time.time;
  }

  void Update() {
    var t = 1 - (Time.time - startTime) / FADE_TIME;
    if (t <= 0) {
      Destroy(this.gameObject);
      return;
    }
    for (int i = 0; i < renderers.Length; i++) {
      var originalColor = originalColors[i];
      var newColor = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a * t);
      renderers[i].color = newColor;
    }
  }
}
