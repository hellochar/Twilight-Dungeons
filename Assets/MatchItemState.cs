using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class MatchItemState : MonoBehaviour {
  public Item item;
  private Button button;
  private Image image;
  private TMPro.TMP_Text stacksText;
  public SpriteAtlas masterSpriteAtlas;
  private GameObject detailsPopupPrefab;

  void Start() {
    detailsPopupPrefab = Resources.Load<GameObject>("UI/Item Details Popup");
    /// on click - toggle the popup for this item
    button = GetComponent<Button>();
    button.onClick.AddListener(HandleItemClicked);

    image = GetComponentInChildren<Image>(true);
    image.sprite = ItemInfo.GetSpriteForItem(item);
    image.rectTransform.sizeDelta = image.sprite.rect.size * 3;

    stacksText = GetComponentInChildren<TMPro.TMP_Text>(true);
    stacksText.gameObject.SetActive(item is IStackable);
  }

  private void HandleItemClicked() {
    GameObject inventoryContainer = GetComponentInParent<Canvas>().transform.Find("Inventory Container").gameObject;
    var detailsPopup = Instantiate(detailsPopupPrefab, new Vector3(), Quaternion.identity, inventoryContainer.transform);
    var popupMatchItem = detailsPopup.GetComponent<PopupMatchItem>();
    popupMatchItem.item = item;
    popupMatchItem.spriteBase = image.gameObject;

    // take up the whole canvas
    var rectTransform = detailsPopup.GetComponent<RectTransform>();
    rectTransform.offsetMax = new Vector2();
    rectTransform.offsetMin = new Vector2();
  }

  // Update is called once per frame
  void Update() {
    if (stacksText.isActiveAndEnabled) {
      stacksText.text = ((IStackable) item).stacks.ToString();
    }
  }
}
