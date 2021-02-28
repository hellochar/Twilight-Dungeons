using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupController : MonoBehaviour {
  public event Action OnClose;
  public GameObject errorContainer;

  void Start() {
    // fill up the parent
    var rectTransform = GetComponent<RectTransform>();
    rectTransform.offsetMax = new Vector2();
    rectTransform.offsetMin = new Vector2();

    AudioClipStore.main?.popupOpen.Play(0.2f);
  }

  public void SetErrorText(string errorText) {
    errorContainer.SetActive(errorText != null);
    var text = errorContainer.transform.Find("Viewport/Content/Text").GetComponent<TMPro.TMP_Text>();
    text.text = errorText;
  }

  void OnDestroy() {
    OnClose?.Invoke();
    AudioClipStore.main?.popupClose.Play(0.2f);
  }

  public void Close() {
    Destroy(this.gameObject);
  }
}
