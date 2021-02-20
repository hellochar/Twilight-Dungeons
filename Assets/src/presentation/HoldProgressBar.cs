using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HoldProgressBar : MonoBehaviour {
  public static HoldProgressBar main;
  Image image;

  void Awake() {
    main = this;
    image = GetComponent<Image>();
    gameObject.SetActive(false);
  }

  public void HoldStart() {
    gameObject.SetActive(true);
    image.fillAmount = 0;
  }

  public void HoldUpdate(float percentDone) {
    float fillAmount = 0;
    if (percentDone < 0.25) {
      fillAmount = 0;
    } else {
      fillAmount = EasingFunctions.EaseInOutSine(0, 1, (percentDone - 0.25f) / 0.75f);
    }
    image.fillAmount = fillAmount;
  }

  public void HoldEnd() {
    gameObject.SetActive(false);
  }
}