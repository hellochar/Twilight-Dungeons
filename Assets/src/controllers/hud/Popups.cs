using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class Popups {
  public static PopupController CreateEmpty(TextAnchor alignment = TextAnchor.MiddleCenter) {
    var parent = GameObject.Find("Canvas").transform;
    var popup = UnityEngine.Object.Instantiate(PrefabCache.UI.GetPrefabFor("Popup"), new Vector3(), Quaternion.identity, parent);
    var controller = popup.GetComponent<PopupController>();
    controller.Init(alignment);
    return controller;
  }

  public static PopupController CreateStandard(
    string title,
    string category,
    string info,
    string flavor,
    GameObject sprite = null,
    List<(string, Action)> buttons = null,
    string errorText = null,
    Inventory inventory = null,
    Entity entity = null,
    string prefab = "StandardPopupContent"
  ) {
    var controller = CreateEmpty();
    GameObject popup = controller.gameObject;

    GameObject content = UnityEngine.Object.Instantiate(PrefabCache.UI.GetPrefabFor(prefab), controller.container);
    content.name = "Content";

    /// TODO refactor into a Controller class
    var titleText = content.transform.Find("Title").GetComponent<TMPro.TMP_Text>();
    if (title == null) {
      titleText.gameObject.SetActive(false);
      content.transform.Find("Bottom Decorater Positioner").gameObject.SetActive(false);
    } else {
      titleText.text = title;
    }

    var categoryText = content.transform.Find("Title/Category").GetComponent<TMPro.TMP_Text>();
    categoryText.text = category;

    var infoText = content.transform.Find("Stats").GetComponent<TMPro.TMP_Text>();
    infoText.text = info;

    var errorContainer = content.transform.Find("Scroll View").gameObject;
    errorContainer.SetActive(errorText != null);
    var text = errorContainer.transform.Find("Viewport/Content/Text").GetComponent<TMPro.TMP_Text>();
    text.text = errorText;

    var flavorText = content.transform.Find("Flavor Text").GetComponent<TMPro.TMP_Text>();
    flavorText.text = flavor;

    // Add sprite
    var spriteContainer = content.transform.Find("Sprite Container").gameObject;
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
    var buttonsContainer = content.transform.Find("Actions");
    if (buttons != null && buttons.Count > 0) {
      buttons.ForEach((b) => MakeButton(b.Item1, b.Item2, buttonsContainer, popup));
    } else {
      content.transform.Find("Space").gameObject.SetActive(false);
      buttonsContainer.gameObject.SetActive(false);
      // if there's no actions, clicking the frame itself will toggle the popup
      var frameButton = content.AddComponent<Button>();
      frameButton.onClick.AddListener(controller.Close);
    }

    if (inventory != null) {
      var inventoryContainer = content.transform.Find("Inventory Container").gameObject;
      inventoryContainer.SetActive(true);
      var inventoryController = inventoryContainer.GetComponentInChildren<InventoryController>();
      inventoryController.inventory = inventory;
      // popup.transform.Find("Overlay").gameObject.SetActive(false);

      controller.showPlayerInventoryOnTop = true;
    }

    return controller;
  }

  private static GameObject MakeButton(string name, Action onClicked, Transform parent, GameObject popup) {
    var button = UnityEngine.Object.Instantiate(PrefabCache.UI.GetPrefabFor("Action Button"), new Vector3(), Quaternion.identity, parent);
    button.GetComponentInChildren<TMPro.TMP_Text>().text = name;
    button.GetComponent<Button>().onClick.AddListener(
      new UnityEngine.Events.UnityAction(() => {
        PlayerController.current.PerformPlayerAction(onClicked);
        UnityEngine.Object.Destroy(popup);
      })
    );

    button.name = name;
    return button;
  }
}