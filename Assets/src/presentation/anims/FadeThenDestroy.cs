using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Fade all renderers in this hierarchy over 1 second and then Destroy() the gameObject it's attached to
public class FadeThenDestroy : MonoBehaviour {
  public float fadeTime = 0.5f;
  public float shrink = 0.5f;
  private SpriteRenderer[] renderers;
  private Image[] images;
  private Color[] colors;
  private Vector3[] scales;
  float startTime;
  void Start() {
    renderers = GetComponentsInChildren<SpriteRenderer>();
    images = GetComponentsInChildren<Image>();
    if (renderers.Length > 0) {
      colors = new Color[renderers.Length];
      scales = new Vector3[renderers.Length];
      for (int i = 0; i < renderers.Length; i++) {
        colors[i] = renderers[i].color;
        scales[i] = renderers[i].transform.localScale;
      }
    } else {
      colors = new Color[images.Length];
      scales = new Vector3[images.Length];
      for (int i = 0; i < images.Length; i++) {
        colors[i] = images[i].color;
        scales[i] = images[i].transform.localScale;
      }
    }
    startTime = Time.time;
  }

  void Update() {
    var t = (Time.time - startTime) / fadeTime;
    if (t >= 1) {
      Destroy(this.gameObject);
      return;
    }
    if (renderers.Length > 0) {
      for (int i = 0; i < renderers.Length; i++) {
        if (renderers[i] != null) {
          var originalColor = colors[i];
          var newColor = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a * (1 - t));
          var newScale = scales[i] * (1 - shrink * t);
          renderers[i].color = newColor;
          renderers[i].transform.localScale = newScale;
        }
      }
    } else {
      for (int i = 0; i < images.Length; i++) {
        if (images[i] != null) {
          var originalColor = colors[i];
          var newColor = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a * (1 - t));
          var newScale = scales[i] * (1 - shrink * t);
          images[i].color = newColor;
          images[i].transform.localScale = newScale;
        }
      }
    }
  }
}
