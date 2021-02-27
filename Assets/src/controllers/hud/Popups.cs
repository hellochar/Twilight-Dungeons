using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class Popups {
  private static GameObject PopupPrefab;

  public static GameObject Create(
    string title,
    string category,
    string info,
    string flavor,
    GameObject sprite = null,
    Transform parent = null,
    List<GameObject> buttons = null
  ) {
    GameObject popup = InstantiatePopup(parent);
    var controller = popup.GetComponent<PopupController>();

    /// TODO refactor into a Controller class
    var titleText = popup.transform.Find("Frame/Title").GetComponent<TMPro.TMP_Text>();
    if (title == null) {
      titleText.gameObject.SetActive(false);
    } else {
      titleText.text = title;
    }

    var categoryText = popup.transform.Find("Frame/Title/Category").GetComponent<TMPro.TMP_Text>();
    categoryText.text = category;

    var infoText = popup.transform.Find("Frame/Stats").GetComponent<TMPro.TMP_Text>();
    infoText.text = info;

    var flavorText = popup.transform.Find("Frame/Flavor Text").GetComponent<TMPro.TMP_Text>();
    flavorText.text = flavor;

    // Add sprite
    var spriteContainer = popup.transform.Find("Frame/Sprite Container").gameObject;
    if (sprite == null) {
      spriteContainer.SetActive(false);
    } else {
      var spriteGameObject = UnityEngine.Object.Instantiate(sprite, spriteContainer.GetComponent<RectTransform>().position, Quaternion.identity, spriteContainer.transform);
      RectTransform spriteRectTransform = spriteGameObject.GetComponent<RectTransform>();
      spriteRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
      spriteRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
      spriteRectTransform.sizeDelta = new Vector2(48, 48);
    }

    // Add buttons
    var buttonsContainer = popup.transform.Find("Frame/Actions");
    if (buttons != null && buttons.Count > 0) {
      buttons.ForEach((b) => {
        b.transform.SetParent(buttonsContainer, false);
      });
    } else {
      buttonsContainer.gameObject.SetActive(false);
      // if there's no actions, clicking the frame itself will toggle the popup
      var frame = popup.transform.Find("Frame").gameObject;
      var frameButton = frame.AddComponent<Button>();
      frameButton.onClick.AddListener(controller.Close);
    }

    return popup;
  }

  private static GameObject InstantiatePopup(Transform parent) {
    if (PopupPrefab == null) {
      PopupPrefab = Resources.Load<GameObject>("UI/Popup");
    }
    if (parent == null) {
      parent = GameObject.Find("Canvas").transform;
    }
    return UnityEngine.Object.Instantiate(PopupPrefab, new Vector3(), Quaternion.identity, parent);
  }
}