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

    if (item is ItemSeed seed) {
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

      if (item is ITargetedAction<Entity> targetedAction) {
        var name = targetedAction.TargettedActionName;
        Action action = () => targetedAction.ShowTargetingUIThenPerform(player);
        buttons.Insert(0, (name, action));
      }

      if (player.floor.depth == 0) {
        var playerActions = item.GetType().GetMethods().Where(m => m.GetCustomAttributes(typeof(PlayerActionAttribute), true).Any());
        foreach(var action in playerActions) {
          buttons.Add((Util.WithSpaces(action.Name), () => {
            action.Invoke(item, new object[0]);
          }));
        }
      }
    }

    popup = Popups.CreateStandard(
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

  // Update is called once per frame
  void Update() {
    if (item.disjoint) {
      if (item.stacks < item.stacksMax) {
        stacksText.text = $"{item.stacks}/{item.stacksMax}";
      } else {
        stacksText.text = "";
      }
    } else if (item.stacksMax > 1) {
      stacksText.text = item.stacks.ToString();
    } else {
      stacksText.gameObject.SetActive(false);
    }
  }

  void IDragHandler.OnDrag(PointerEventData eventData) {
    if (!item.inventory.allowDragAndDrop) {
      return;
    }
    // doesn't fully work; doesn't account for scaling of parents
    // Vector2 offset = eventData.position - eventData.pressPosition;

    // Debug.Log("OnDrag " + offset);
    // (transform as RectTransform).anchoredPosition = offset;

    // works
    transform.Translate(eventData.delta);
  }

  int dragStartLayer;
  void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) {
    if (!item.inventory.allowDragAndDrop) {
      eventData.pointerDrag = null;
      return;
    }
    button.enabled = false;
    InventorySlotController.BeginDragging(item);
    dragStartLayer = gameObject.layer;
    gameObject.layer = Physics.IgnoreRaycastLayer;
    GetComponentInChildren<Graphic>().raycastTarget = false;
    // Debug.Log("OnBeginDrag");
  }

  void IEndDragHandler.OnEndDrag(PointerEventData eventData) {
    if (!item.inventory.allowDragAndDrop) {
      return;
    }
    GetComponentInChildren<Graphic>().raycastTarget = true;
    InventorySlotController.EndDragging(item);
    gameObject.layer = dragStartLayer;
    (transform as RectTransform).anchoredPosition = new Vector2();
    button.enabled = true;
    // Debug.Log("OnEndDrag");
  }
}
