using System;
using System.Collections;
using UnityEngine;

public static class Highlights {
  public static GameObject Create(Entity e, Func<bool> destroyWhen = null) {
    var highlight = PrefabCache.UI.Instantiate("Highlight");

    GameModelController.main.StartCoroutine(CheckDestroy());

    IEnumerator CheckDestroy() {
      while (true) {
        highlight.transform.position = Util.withZ(e.pos);
        if (highlight == null || e.IsDead || (destroyWhen?.Invoke() ?? false)) {
          break;
        }
        yield return new WaitForEndOfFrame();
      }

      highlight.AddComponent<FadeThenDestroy>().fadeTime = 0.2f;
    }

    return highlight;
  }
}