using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupController : MonoBehaviour, ICameraOverride {
  // set externally
  public Entity target;
  public event Action OnClose;
  internal bool showPlayerInventoryOnTop;

  public Transform container;
  public GameObject overlay, overlayLeanLeft, overlayLeanRight;
  public HorizontalLayoutGroup horizontalLayoutGroup;
  public CanvasGroup canvasGroup;


  private Transform playerInventoryOriginalParent;
  private int playerInventoryOriginalSiblingIndex;

  public CameraState overrideState =>
    horizontalLayoutGroup.childAlignment == TextAnchor.MiddleCenter ?
    null : 
    new CameraState {
      lean = ViewportLean.Left,
      target = this.target ?? GameModel.main.player,
    };

  void Start() {
    StartCoroutine(Transitions.Animate(0.5f, t => {
      canvasGroup.alpha = t;
      container.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
    }, null, EasingFunctions.EaseOutExpo));
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

  void Update() {
    if (target != null) {
      var infoText = container.transform.Find("Content/Stats")?.GetComponent<TMPro.TMP_Text>();
      if (infoText) {
        infoText.text = target.description;
      }
    }
  }

  public void Init(TextAnchor alignment) {
    if (alignment != TextAnchor.MiddleCenter) {
      overlay.GetComponent<Image>().color = Color.clear;
    }
    overlayLeanLeft.SetActive(alignment == TextAnchor.MiddleLeft);
    overlayLeanRight.SetActive(alignment == TextAnchor.MiddleRight);
    horizontalLayoutGroup.childAlignment = alignment;
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
