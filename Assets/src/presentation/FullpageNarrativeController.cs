using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FullpageNarrativeController : MonoBehaviour, IPointerClickHandler {
  public GameObject tapPrompt;
  public AudioSource source;

  private string copyFull;

  public IEnumerator PlayNarrative(Action then = null) {
    var textComponent = GetComponentInChildren<TMPro.TMP_Text>();
    copyFull = textComponent.text;

    textComponent.text = "";
    gameObject.SetActive(true);
    yield return new WaitForSeconds(0.1f);
    var prologuePages = copyFull.Split(new string[] { "---" }, System.StringSplitOptions.RemoveEmptyEntries);
    foreach (var page in prologuePages) {
      yield return StartCoroutine(ShowPage(textComponent, page.Trim()));
      /// Pause for a bit after the page stops.
      yield return new WaitForSeconds(0.05f);
    }

    // Turn text to black.
    yield return StartCoroutine(FadeText());

    if (then != null) {
      then.Invoke();
    }
  }

  IEnumerator FadeText() {
    var textComponent = GetComponentInChildren<TMPro.TMP_Text>();
    return Transitions.Animate(2, (t) => {
      textComponent.color = Color.Lerp(new Color(1, 1, 1, 1), new Color(1, 1, 1, 0), t);
    });
  }

  // wait for player to touch
  IEnumerator ShowPage(TMPro.TMP_Text component, string text) {
    // var paragraphs = text.Split(new string[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries);
    // foreach (var paragraph in paragraphs) {
    //   yield return new WaitForSeconds(0.05f);
    // }
    for (var length = 0; length <= text.Length; length++) {
      if (hasClicked) {
        hasClicked = false;
        length = text.Length;
      }
      component.text = text.Substring(0, length);
      if (length > 0 && text[length - 1] == '\n') {
        yield return StartCoroutine(WaitForTap(.5f));
      } else {
        if (!source.isPlaying) {
          source.Play();
        }
        yield return new WaitForSeconds(0.035f);
      }
    }

    var waitStart = Time.time;
    // wait for tap to continue
    while (true) {
      /// it's been over 1 second since a tap
      if (Time.time - waitStart > 1f) {
        tapPrompt.SetActive(true);
        // tapPrompt.GetComponent<Animator>().Play("motion", -1, 0);
      }
      if (hasClicked) {
        hasClicked = false;
        tapPrompt.SetActive(false);
        yield break;
      }
      yield return null;
    }
  }

  // do NOT consume the hasClicked
  IEnumerator WaitForTap(float duration) {
    var start = Time.time;
    var t = 0f;
    do {
      t = (Time.time - start) / duration;
      if (hasClicked) {
        yield break;
      }
      yield return new WaitForEndOfFrame();
    } while (t < 1);
  }

  private bool hasClicked = false;
  public void OnPointerClick(PointerEventData eventData) {
    hasClicked = true;
  }
}
