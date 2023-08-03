using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Highlights {
  public static List<GameObject> all = new List<GameObject>();
  public static GameObject CreateUI(GameObject go, Func<bool> destroyWhen = null) {
    var highlight = PrefabCache.UI.Instantiate("HighlightUI", go.transform);
    var rtt = highlight.GetComponent<RectTransform>();
    rtt.anchoredPosition = Vector2Int.zero;
    all.Add(highlight);

    GameModelController.main.StartCoroutine(CheckDestroy());

    IEnumerator CheckDestroy() {
      while (true) {
        if (go == null || highlight == null) {
          // don't FadeThenDestroy highlight; highlight is already gone
          yield break;
        }
        if (destroyWhen != null && destroyWhen()) {
          break;
        }
        rtt.anchoredPosition = Vector2Int.zero;
        yield return new WaitForSeconds(0.1f);
      }

      if (highlight) {
        all.Remove(highlight);
        highlight.AddComponent<FadeThenDestroy>().fadeTime = 0.2f;
      }
    }
    return highlight;
  }

  public static GameObject Create(Entity e, Func<bool> destroyWhen = null) {
    if (e == null) {
      return null;
    }
    var highlight = PrefabCache.UI.Instantiate("Highlight");
    all.Add(highlight);

    GameModelController.main.StartCoroutine(CheckDestroy());

    IEnumerator CheckDestroy() {
      while (true) {
        if (highlight == null || e.IsDead || (destroyWhen?.Invoke() ?? false)) {
          break;
        }
        highlight.transform.position = Util.withZ(e.pos);
        yield return new WaitForSeconds(0.1f);
      }

      if (highlight) {
        all.Remove(highlight);
        highlight.AddComponent<FadeThenDestroy>().fadeTime = 0.2f;
      }
    }

    return highlight;
  }

  internal static void RemoveAll() {
    foreach (var highlight in all) {
      if (highlight != null) {
        highlight.AddComponent<FadeThenDestroy>();
      }
    }
    all.Clear();
  }
}