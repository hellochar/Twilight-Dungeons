using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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
      ActionButtonPrefab = Resources.Load<GameObject>("UI/Action Button");
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
    Player player = GameModel.main.player;
    List<ActorTask> playerTasks = item.GetAvailableTasks(player);

    // put more fundamental actions later
    playerTasks.Reverse();

    var popupActions = playerTasks.Select((task) => new Action(() => {
      player.task = task;
    })).ToList();

    GameObject popup = null;

    var buttons = playerTasks.Select((task) => {
      var button = Instantiate(ActionButtonPrefab, new Vector3(), Quaternion.identity);
      button.GetComponentInChildren<TMPro.TMP_Text>().text = task.Name;
      button.GetComponent<Button>().onClick.AddListener(() => {
        player.task = task;
        Destroy(popup);
      });
      return button;
    }).ToList();
    if (item is ItemSeed seed) {
      var button = Instantiate(ActionButtonPrefab, new Vector3(), Quaternion.identity);
      button.GetComponentInChildren<TMPro.TMP_Text>().text = "Plant";
      button.GetComponent<Button>().onClick.AddListener(() => {
        PlantWithUI(seed, player, popup);
        Destroy(popup);
      });

      buttons.Insert(0, button);
    }

    popup = Popups.Create(
      title: item.displayName,
      info: item.GetStatsFull(),
      flavor: ObjectInfo.GetFlavorTextFor(item),
      sprite: image.gameObject,
      buttons: buttons
    );
    var popupMatchItem = popup.AddComponent<ItemPopupController>();
    popupMatchItem.item = item;
  }

  public async void PlantWithUI(ItemSeed seed, Player player, GameObject popup) {
    CloseInventory();
    popup.SetActive(false);
    try {
      var soil = await MapSelector.Select(
        GameModel.main.currentFloor.tiles.Where(tile => tile is Soil && tile.isVisible && tile.CanBeOccupied()).Cast<Soil>()
      );
      player.SetTasks(
        new MoveNextToTargetTask(player, soil.pos),
        new GenericTask(player, (p) => {
          if (p.IsNextTo(soil)) {
            seed.Plant(soil);
          }
        })
      );
    } catch (PlayerSelectCanceledException) {
      // if player cancels selection, go back to before
      OpenInventory();
      popup.SetActive(true);
    }
  }

  public void OpenInventory() {
    /// suuuper hack
    GameObject.Find("Canvas")
      .GetComponentsInChildren<Transform>(true)
      .First((c) => c.gameObject.name == "Inventory Container")
      .gameObject.SetActive(true);
  }

  public void CloseInventory() {
    GameObject.Find("Inventory Container").SetActive(false);
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
