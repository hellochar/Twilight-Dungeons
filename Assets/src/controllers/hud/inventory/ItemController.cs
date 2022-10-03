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
  public Image itemImage;
  private TMPro.TMP_Text stacksText;
  private Button button;

  void Start() {
    stacksText = GetComponentInChildren<TMPro.TMP_Text>(true);
    /// on click - toggle the popup for this item
    button = GetComponent<Button>();
    button.onClick.AddListener(HandleItemClicked);

    var wantedSprite = ObjectInfo.GetSpriteFor(item);
    if (wantedSprite != null) {
      itemImage.sprite = wantedSprite;
      if (item is ItemVisibleBox) {
        itemImage.color = Color.grey;
      }
    }

    if (item is ItemGrass grass) {
      var type = grass.grassType;
      var typePath =
        type.IsSubclassOf(typeof(Grass)) ?  "Grasses" :
        type.IsSubclassOf(typeof(Tile)) ? "Tiles" :
        type.IsSubclassOf(typeof(Body)) ? "Actors" :
        type.IsSubclassOf(typeof(Plant)) ? "Plants" :
        "null";
      var prefab = Resources.Load<GameObject>($"Entities/{typePath}/{type.Name}");
      var renderer = prefab.GetComponentInChildren<SpriteRenderer>();
      itemImage.sprite = renderer.sprite;
      itemImage.color = renderer.color;
    }

    /// HACK set color for fertilizer
    if (item is ItemFertilizer fertilizer) {
      string resourcePath = $"Entities/Actors/{fertilizer.aiActorType.Name}";
      var prefab = Resources.Load<GameObject>(resourcePath);
      if (prefab != null) {
        var color = prefab.GetComponent<ActorController>().bloodColor;
        color.a = 1;
        itemImage.color = color;
      }
    }

    if (item is ItemSeed seed && GameModel.main.player.inventory.HasItem(item)) {
      var plantType = seed.plantType;
      var plantPrefab = Resources.Load<GameObject>($"Entities/Plants/{plantType.Name}");
      var maturePlant = plantPrefab.transform.Find("Mature");
      var renderer = maturePlant.GetComponent<SpriteRenderer>();

      itemImage.sprite = renderer.sprite;
      itemImage.color = renderer.color;
    }

    Update();
  }

  private void HandleItemClicked() {
    ShowItemPopup(item, itemImage.gameObject);
  }

  public static void ShowItemPopup(Item item, GameObject image) {
    GameObject popup = null;
    List<(string, Action)> buttons = null;

    Player player = GameModel.main.player;
    if (item.inventory == player.inventory || item.inventory == player.equipment) {
      List<MethodInfo> methods = item.GetAvailableMethods(player);

      // put more fundamental actions later
      methods.Reverse();

      buttons = methods.Select((method) => {
        Action action = () => {
          try {
            method.Invoke(item, new object[] { player });
            GameModel.main.DrainEventQueue();
          } catch (TargetInvocationException outer) {
            if (outer.InnerException is CannotPerformActionException e) {
              GameModel.main.turnManager.OnPlayerCannotPerform(e);
            }
            throw outer;
          }
        }; 
        return (method.Name, action);
      }).ToList();

      if (item is ITargetedAction<Entity> selectorUI) {
        var name = selectorUI.TargettedActionName;
        Action action = () => ShowTargetingUIThenPerform(selectorUI, player);
        buttons.Insert(0, (name, action));
      }
    }

    popup = Popups.Create(
      title: item.displayName,
      category: GetCategoryForItem(item),
      info: item.GetStatsFull(),
      flavor: ObjectInfo.GetFlavorTextFor(item),
      sprite: image,
      buttons: buttons
    ).gameObject;
    var popupMatchItem = popup.AddComponent<ItemPopupController>();
    popupMatchItem.item = item;
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

  public static async void ShowTargetingUIThenPerform<T>(ITargetedAction<T> item, Player player) where T : Entity {
    var floor = player.floor;
    try {
      var target = await MapSelector.SelectUI(item.Targets(player));
      item.PerformTargettedAction(player, target);
      GameModel.main.DrainEventQueue();
    } catch (PlayerSelectCanceledException) {
    } catch (CannotPerformActionException e) {
      GameModel.main.turnManager.OnPlayerCannotPerform(e);
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

  void IDragHandler.OnDrag(PointerEventData eventData) {
    // doesn't fully work; doesn't account for scaling of parents
    // Vector2 offset = eventData.position - eventData.pressPosition;

    // Debug.Log("OnDrag " + offset);
    // (transform as RectTransform).anchoredPosition = offset;

    // works
    transform.Translate(eventData.delta);
  }

  int dragStartLayer;
  void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) {
    button.enabled = false;
    InventorySlotController.BeginDragging(item);
    dragStartLayer = gameObject.layer;
    gameObject.layer = Physics.IgnoreRaycastLayer;
    GetComponentInChildren<Graphic>().raycastTarget = false;
    Debug.Log("OnBeginDrag");
  }

  void IEndDragHandler.OnEndDrag(PointerEventData eventData) {
    GetComponentInChildren<Graphic>().raycastTarget = true;
    InventorySlotController.EndDragging(item);
    gameObject.layer = dragStartLayer;
    (transform as RectTransform).anchoredPosition = new Vector2();
    button.enabled = true;
    Debug.Log("OnEndDrag");
  }
}
