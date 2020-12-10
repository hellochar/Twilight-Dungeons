using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

/// Renders one Item in the UI.
public class ItemController : MonoBehaviour {
  private static GameObject ActionButtonPrefab;
  public Item item;
  private Button button;
  private Image image;
  private TMPro.TMP_Text stacksText;

  void Start() {
    if (ActionButtonPrefab == null) {
      ActionButtonPrefab = Resources.Load<GameObject>("UI/Item Action Button");
    }
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

    Player player = item.inventory.Player;
    List<ActorTask> playerTasks = item.GetAvailableTasks(player);

    // put more fundamental actions later
    playerTasks.Reverse();

    var popupActions = playerTasks.Select((task) => new Action(() => {
      player.task = task;
    })).ToList();

    GameObject popup = null;

    var buttons = playerTasks.Select((task) => {
      var button = Instantiate(ActionButtonPrefab, new Vector3(), Quaternion.identity);
      button.GetComponentInChildren<TMPro.TMP_Text>().text = task.displayName;
      button.GetComponent<Button>().onClick.AddListener(() => {
        player.task = task;
        Destroy(popup);
      });
      return button;
    }).ToList();
    popup = Popups.Create(
      title: item.displayName,
      info: item.GetStatsFull(),
      flavor: ObjectInfo.GetFlavorTextFor(item),
      sprite: image.gameObject,
      buttons: buttons
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
