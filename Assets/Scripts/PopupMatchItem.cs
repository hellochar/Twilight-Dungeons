using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PopupMatchItem : MonoBehaviour {
  public Item item;

  GameObject itemActionButtonPrefab;

  TMPro.TMP_Text title;
  GameObject actionsContainer;
  TMPro.TMP_Text stats;

  void Start() {
    itemActionButtonPrefab = Resources.Load<GameObject>("UI/Item Action Button");
    actionsContainer = transform.Find("Frame/Actions").gameObject;
    stats = transform.Find("Frame/Stats").GetComponent<TMPro.TMP_Text>();

    Player player = GameModel.main.player;
    List<ActorTask> actions = item.GetAvailableTasks(player);
    if (actions.Any()) {
      // put more fundamental actions later
      actions.Reverse();
      foreach (var action in actions) {
        var actionButton = Instantiate(itemActionButtonPrefab, new Vector3(), Quaternion.identity, actionsContainer.transform);
        actionButton.GetComponentInChildren<TMPro.TMP_Text>().text = action.displayName;
        actionButton.GetComponent<Button>().onClick.AddListener(() => {
          player.task = action;
          Close();
          // CloseInventory();
        });
      }
    } else {
      actionsContainer.SetActive(false);
    }
  }

  // Update is called once per frame
  void Update() {
    stats.text = item.GetStatsFull();
    // if it's been removed
    if (item.inventory == null) {
      Debug.LogWarning("Item Details popup is being run on an item that's been removed from the inventory!");
      Destroy(this);
      return;
    }
  }

  /// call to close the popup
  public void Close() {
    Destroy(gameObject);
  }

  public void CloseInventory() {
    var inventoryContainer = transform.parent.gameObject;
    inventoryContainer.SetActive(false);
  }
}
