using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupController : MonoBehaviour {
  public event Action OnClose;
  public GameObject errorContainer;
  public GameObject inventoryContainer;
  internal bool showPlayerInventoryOnTop;

  private Transform playerInventoryOriginalParent;
  private int playerInventoryOriginalSiblingIndex;
  void Start() {
    // fill up the parent
    var rectTransform = GetComponent<RectTransform>();
    rectTransform.offsetMax = new Vector2();
    rectTransform.offsetMin = new Vector2();

    AudioClipStore.main?.popupOpen.Play(0.2f);

    if (showPlayerInventoryOnTop) {
      var playerInventory = GameObject.Find("Inventory Container");

      playerInventoryOriginalParent = playerInventory.transform.parent;
      playerInventoryOriginalSiblingIndex = playerInventory.transform.GetSiblingIndex();

      // move player inventory right ahead of this popup
      playerInventory.transform.SetParent(transform.parent);
      playerInventory.transform.SetAsLastSibling();
    }
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
    if (showPlayerInventoryOnTop) {
      var playerInventory = GameObject.Find("Inventory Container");
      playerInventory.transform.SetParent(playerInventoryOriginalParent);
      playerInventory.transform.SetSiblingIndex(playerInventoryOriginalSiblingIndex);
    }
  }
}
