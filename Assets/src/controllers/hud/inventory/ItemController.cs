using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

/// Renders one Item in the UI.
public class ItemController : MonoBehaviour {
  [NonSerialized]
  public Item item;
  public GameObject maturePlantBackground;
  public Image itemImage;
  private TMPro.TMP_Text stacksText;

  void Start() {
    /// on click - toggle the popup for this item
    GetComponent<Button>().onClick.AddListener(HandleItemClicked);

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
    } catch (PlayerSelectCanceledException) {}
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
