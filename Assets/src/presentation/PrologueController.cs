using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class PrologueController : MonoBehaviour {
  public GameObject tapPrompt;

  void Start() {
    if (HasPlayed()) {
      gameObject.SetActive(false);
    } else {
      Play();
    }
  }

  void Play() {
    // PlayerPrefs.SetInt("hasSeenPrologue", 1);
    StartCoroutine(PlayPrologueAsync());
    gameObject.SetActive(true);
  }

  IEnumerator PlayPrologueAsync() {
    var textComponent = GetComponentInChildren<TMPro.TMP_Text>();
    var prologueCopyFull = textComponent.text;
    var prologuePages = prologueCopyFull.Split(new string[] { "---" }, System.StringSplitOptions.None);
    foreach (var page in prologuePages) {
      yield return StartCoroutine(ShowPage(textComponent, page));
      /// pause for a bit after the page stops
      yield return new WaitForSeconds(0.05f);
    }
    // turn text to black
    yield return StartCoroutine(FadeText());
    yield return StartCoroutine(LoadMainScene.FadeTo(GetComponent<Image>(), 2, new Color(0, 0, 0, 0)));
    gameObject.SetActive(false);
  }

  IEnumerator FadeText() {
    var textComponent = GetComponentInChildren<TMPro.TMP_Text>();
    return LoadMainScene.AnimateLinear(2, (t) => {
      textComponent.color = Color.Lerp(new Color(1, 1, 1, 1), new Color(1, 1, 1, 0), t);
    });
  }

  // wait for player to touch
  IEnumerator ShowPage(TMPro.TMP_Text component, string text) {
    for (var length = 0; length < text.Length; length++) {
      if (Input.GetMouseButton(0)) {
        length = text.Length;
      }
      component.text = text.Substring(0, length);
      yield return new WaitForSeconds(0.05f);
    }

    var waitStart = Time.time;
    // wait for tap to continue
    while (true) {
      /// it's been over 1 second since a tap
      if (Time.time - waitStart > 1f) {
        tapPrompt.SetActive(true);
        // tapPrompt.GetComponent<Animator>().Play("motion", -1, 0);
      }
      if (Input.GetMouseButtonDown(0)) {
        tapPrompt.SetActive(false);
        yield break;
      }
      yield return null;
    }
  }

  bool HasPlayed() {
    return PlayerPrefs.HasKey("hasSeenPrologue");
  }

  // Update is called once per frame
  void Update() {

  }
}
