using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupController : MonoBehaviour {
  void Start() {
    // fill up the parent
    var rectTransform = GetComponent<RectTransform>();
    rectTransform.offsetMax = new Vector2();
    rectTransform.offsetMin = new Vector2();

    AudioClipStore.main.popupOpen.Play(0.2f);
  }

  void OnDestroy() {
    AudioClipStore.main.popupClose.Play(0.2f);
  }

  public void Close() {
    Destroy(this.gameObject);
  }
}
