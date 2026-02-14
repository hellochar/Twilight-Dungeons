using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using UnityEngine.UI;

/// Renders one Item in the UI.
public class ItemController : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler {
  [NonSerialized]
  public Item item;
  public GameObject maturePlantBackground;
  public Image itemImage;
  private TMPro.TMP_Text stacksText;

  [NonSerialized]
  public GameObject popup = null;
  private Button button;

  void Start() {
    /// on click - toggle the popup for this item
    button = GetComponent<Button>();
    button.onClick.AddListener(HandleItemClicked);

    var wantedSprite = ObjectInfo.GetSpriteFor(item);
    if (wantedSprite != null) {
      itemImage.sprite = wantedSprite;
    }

    if (item is ItemSeed seed && GameModel.main.player.inventory.HasItem(item)) {
      var plantType = seed.plantType;
      var plantPrefab = Resources.Load<GameObject>($"Entities/Plants/{plantType.Name}");
      var maturePlant = plantPrefab.transform.Find("Mature");
      var renderer = maturePlant.GetComponent<SpriteRenderer>();
      maturePlantBackground.GetComponent<Image>().sprite = renderer.sprite;
      var pos = itemImage.transform.localPosition;
      pos.y -= 2;
      itemImage.transform.localPosition = pos;
    } else {
      maturePlantBackground.SetActive(false);
    }

    stacksText = GetComponentInChildren<TMPro.TMP_Text>(true);
    Update();
  }

  private void HandleItemClicked() {
    popup = ShowItemPopup(item, itemImage.gameObject);
  }

  public static GameObject ShowItemPopup(Item item, GameObject image) {
    List<(string, Action)> buttons = null;

    Player player = GameModel.main.player;
    if (item.inventory == player.inventory || item.inventory == player.equipment) {
      List<MethodInfo> methods = item.GetAvailableMethods(player);

      // put more fundamental actions later
      methods.Reverse();

      buttons = methods.Select((method) => {
        Action action = () => {
          method.Invoke(item, new object[] { player });
        };
        return (method.Name, action);
      }).ToList();

      if (item is ITargetedAction<Entity> targetedAction) {
        var name = targetedAction.TargettedActionName;
        Action action = () => targetedAction.ShowTargetingUIThenPerform(player);
        buttons.Insert(0, (name, action));
      }
    }

    var popup = Popups.CreateStandard(
      title: item.displayName,
      category: GetCategoryForItem(item),
      info: item.GetStatsFull(),
      flavor: ObjectInfo.GetFlavorTextFor(item),
      sprite: image,
      buttons: buttons
    ).gameObject;
    var popupMatchItem = popup.AddComponent<ItemPopupController>();
    popupMatchItem.item = item;
    return popup;
  }

  private static string GetCategoryForItem(Item item) {
    switch (item) {
      case EquippableItem e:
        return e.slot.ToString();
      case IEdible e:
        return "Food";
      default:
        return "Item";
    }
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

  private bool CanDragItem() {
    if (item == null) return false;
    if (!item.inventory.allowDragAndDrop) return false;
    // Cannot drag the default Hands item
    if (item is ItemHands) return false;
    // Cannot drag sticky items out of equipment
    if (item is ISticky && item.inventory is Equipment) return false;
    return true;
  }

  void IDragHandler.OnDrag(PointerEventData eventData) {
    if (!CanDragItem()) return;
    transform.Translate(eventData.delta);
  }

  int dragStartLayer;
  void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) {
    if (!CanDragItem()) {
      eventData.pointerDrag = null;
      return;
    }
    button.enabled = false;
    InventorySlotController.BeginDragging(item);
    dragStartLayer = gameObject.layer;
    gameObject.layer = Physics.IgnoreRaycastLayer;
    // Disable raycast on all child graphics so the dragged item doesn't block drops
    foreach (var graphic in GetComponentsInChildren<Graphic>()) {
      graphic.raycastTarget = false;
    }
  }

  void IEndDragHandler.OnEndDrag(PointerEventData eventData) {
    if (!CanDragItem()) return;
    // Re-enable raycast on all child graphics
    foreach (var graphic in GetComponentsInChildren<Graphic>()) {
      graphic.raycastTarget = true;
    }
    InventorySlotController.EndDragging(item);
    gameObject.layer = dragStartLayer;
    (transform as RectTransform).anchoredPosition = new Vector2();
    button.enabled = true;
  }
}
