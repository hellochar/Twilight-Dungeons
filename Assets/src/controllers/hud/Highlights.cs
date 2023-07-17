using System;
using System.Collections;
using UnityEngine;

public static class Highlights {
  public static GameObject CreateUI(GameObject go, Func<bool> destroyWhen = null) {
    var highlight = PrefabCache.UI.Instantiate("HighlightUI", go.transform);

    GameModelController.main.StartCoroutine(CheckDestroy());

    IEnumerator CheckDestroy() {
      while (true) {
        if (go == null) {
          // don't FadeThenDestroy highlight; highlight is already gone
          yield break;
        }
        if (destroyWhen != null && destroyWhen()) {
          break;
        }
        yield return new WaitForSeconds(0.1f);
      }

      highlight.AddComponent<FadeThenDestroy>().fadeTime = 0.2f;
    }
    return highlight;
  }

  public static GameObject Create(Entity e, Func<bool> destroyWhen = null) {
    var highlight = PrefabCache.UI.Instantiate("Highlight");

    GameModelController.main.StartCoroutine(CheckDestroy());

    IEnumerator CheckDestroy() {
      while (true) {
        highlight.transform.position = Util.withZ(e.pos);
        if (highlight == null || e.IsDead || (destroyWhen?.Invoke() ?? false)) {
          break;
        }
        yield return new WaitForSeconds(0.1f);
      }

      highlight.AddComponent<FadeThenDestroy>().fadeTime = 0.2f;
    }

    return highlight;
  }
}