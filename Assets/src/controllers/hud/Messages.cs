using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public static class Messages {

  public static GameObject Create(string text, float duration = 2.8f) {
    var message = PrefabCache.UI.Instantiate("Message");
    message.transform.SetParent(GameObject.Find("Canvas").transform, false);
    var textComponent = message.GetComponentInChildren<TMPro.TMP_Text>();
    textComponent.text = text;
    if (duration > 0) {
      textComponent.StartCoroutine(PauseThenFade());

      IEnumerator PauseThenFade() {
        yield return new WaitForSeconds(duration);
        message.AddComponent<FadeThenDestroy>().fadeTime = 0.2f;
      }
    }

    return message;
  }

  public async static Task<GameObject> CreateDelayed(string text, float delay, float duration = 2.8f) {
    await Task.Delay((int)(delay * 1000));
    return Create(text, duration);
  }
}