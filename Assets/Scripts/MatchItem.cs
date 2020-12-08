using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

/// Renders one Item in the UI.
public class MatchItem : MonoBehaviour {
  public Item item;
  private Button button;
  private Image image;
  private TMPro.TMP_Text stacksText;

  void Start() {
    /// on click - toggle the popup for this item
    button = GetComponent<Button>();
    button.onClick.AddListener(HandleItemClicked);

    image = GetComponentInChildren<Image>(true);
    var wantedSprite = ObjectInfo.GetSpriteFor(item);
    if (wantedSprite != null) {
      image.sprite = wantedSprite;
      image.rectTransform.sizeDelta = wantedSprite.rect.size * 3;
    }

    stacksText = GetComponentInChildren<TMPro.TMP_Text>(true);
    // stacksText.gameObject.SetActive(item is IStackable);
    Update();
  }

  private void HandleItemClicked() {
    GameObject inventoryContainer = GetComponentInParent<Canvas>().transform.Find("Inventory Container").gameObject;
    var popup = Popups.Create(
      parent: inventoryContainer.transform,
      title: item.displayName,
      info: item.GetStatsFull(),
      flavor: ObjectInfo.GetFlavorTextFor(item),
      sprite: image.gameObject
    );
    var popupMatchItem = popup.AddComponent<PopupMatchItem>();
    popupMatchItem.item = item;
  }

  // Update is called once per frame
  void Update() {
    if (item is IStackable i) {
      stacksText.text = i.stacks.ToString();
    } else if (item is IDurable d) {
      if (d.durability < d.maxDurability) {
        stacksText.text = $"{d.durability}/{d.maxDurability}";
      } else {
        stacksText.text = "";
      }
    } else {
      stacksText.gameObject.SetActive(false);
    }
  }
}
