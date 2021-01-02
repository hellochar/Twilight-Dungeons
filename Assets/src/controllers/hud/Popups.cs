using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class Popups {
  private static GameObject PopupPrefab;

  public static GameObject Create(string title, string info, string flavor, GameObject sprite, Transform parent = null, List<GameObject> buttons = null) {
    if (PopupPrefab == null) {
      PopupPrefab = Resources.Load<GameObject>("UI/Popup");
    }
    if (parent == null) {
      parent = GameObject.Find("Canvas").transform;
    }
    var popup = UnityEngine.Object.Instantiate(PopupPrefab, new Vector3(), Quaternion.identity, parent);
    /// TODO refactor into a Controller class

    // fill up the parent
    var rectTransform = popup.GetComponent<RectTransform>();
    rectTransform.offsetMax = new Vector2();
    rectTransform.offsetMin = new Vector2();

    var titleText = popup.transform.Find("Frame/Title").GetComponent<TMPro.TMP_Text>();
    titleText.text = title;

    var infoText = popup.transform.Find("Frame/Stats").GetComponent<TMPro.TMP_Text>();
    infoText.text = info;

    var flavorText = popup.transform.Find("Frame/Flavor Text").GetComponent<TMPro.TMP_Text>();
    flavorText.text = flavor;

    // Add sprite
    var spriteContainer = popup.transform.Find("Frame/Sprite Container").gameObject;
    var spriteGameObject = UnityEngine.Object.Instantiate(sprite, spriteContainer.GetComponent<RectTransform>().position, Quaternion.identity, spriteContainer.transform);
    spriteGameObject.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
    spriteGameObject.GetComponent<RectTransform>().anchorMin = new Vector2();
    spriteGameObject.GetComponent<RectTransform>().sizeDelta = new Vector2();

    // Add buttons
    var buttonsContainer = popup.transform.Find("Frame/Actions");
    if (buttons != null && buttons.Count > 0) {
      buttons.ForEach((b) => {
        b.transform.SetParent(buttonsContainer, false);
      });
    } else {
      buttonsContainer.gameObject.SetActive(false);
    }

    // destroy popup when overlay is clicked
    var overlayButton = popup.transform.Find("Overlay").GetComponent<Button>();
    overlayButton.onClick.AddListener(() => {
      UnityEngine.Object.Destroy(popup);
    });

    return popup;
  }
}