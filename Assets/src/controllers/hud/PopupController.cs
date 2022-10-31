using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupController : MonoBehaviour {
  public Transform container;
  public GameObject overlay;

  public event Action OnClose;
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

  public void Init(TextAnchor alignment) {
    if (alignment != TextAnchor.MiddleCenter) {
      overlay.GetComponent<Image>().color = Color.clear;
    }
    container.GetComponent<HorizontalLayoutGroup>().childAlignment = alignment;
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
