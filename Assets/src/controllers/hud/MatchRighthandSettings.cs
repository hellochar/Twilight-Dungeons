using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// a Rect Transform's base state is:
// Local Scale, Anchored Position, Anchor Min, Anchor Max, SizeDelta, Pivot
public class StoredRectTransform {
  public Vector3 localScale;
  public Vector2 anchoredPosition;
  public Vector2 anchorMin;
  public Vector2 anchorMax;
  public Vector2 sizeDelta;
  public Vector2 pivot;

  public StoredRectTransform(RectTransform transform) {
    localScale = transform.localScale;
    anchoredPosition = transform.anchoredPosition;
    anchorMin = transform.anchorMin;
    anchorMax = transform.anchorMax;
    sizeDelta = transform.sizeDelta;
    pivot = transform.pivot;
  }

  public void Apply(RectTransform transform) {
    transform.localScale = localScale;
    transform.anchoredPosition = anchoredPosition;
    transform.anchorMin = anchorMin;
    transform.anchorMax = anchorMax;
    transform.sizeDelta = sizeDelta;
    transform.pivot = pivot;
  }
}

public class MatchRighthandSettings : MonoBehaviour {
  public RectTransform target;

  private RectTransform rectTransform;
  private StoredRectTransform lefthand;
  private StoredRectTransform righthand;


  // void Awake() {
  //   rectTransform = GetComponent<RectTransform>();
  //   lefthand = new StoredRectTransform(rectTransform);
  //   righthand = new StoredRectTransform(target);
  //   Settings.OnChanged += HandleSettingsChanged;
  // }

  // void Start() {
  //   HandleSettingsChanged();
  // }

  // void OnDestroy() {
  //   Settings.OnChanged -= HandleSettingsChanged;
  // }

  // private void HandleSettingsChanged() {
  //   if (Settings.main.rightHanded) {
  //     righthand.Apply(rectTransform);
  //   } else {
  //     lefthand.Apply(rectTransform);
  //   }
  // }
}
