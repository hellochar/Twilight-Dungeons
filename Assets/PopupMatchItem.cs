using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PopupMatchItem : MonoBehaviour {
  public Item item;
  public GameObject spriteBase;

  GameObject itemActionButtonPrefab;

  TMPro.TMP_Text title;
  GameObject actionsContainer;
  GameObject spriteContainer;
  TMPro.TMP_Text stats;
  TMPro.TMP_Text flavorText;
  void Start() {
    itemActionButtonPrefab = Resources.Load<GameObject>("UI/Item Action Button");
    title = transform.Find("Frame/Title").GetComponent<TMPro.TMP_Text>();
    actionsContainer = transform.Find("Frame/Actions").gameObject;
    spriteContainer = transform.Find("Frame/Sprite Container").gameObject;
    stats = transform.Find("Frame/Stats").GetComponent<TMPro.TMP_Text>();
    flavorText = transform.Find("Frame/Flavor Text").GetComponent<TMPro.TMP_Text>();

    title.text = item.displayName;

    Player player = GameModel.main.player;
    List<ActorAction> actions = item.GetAvailableActions(player);
    if (actions.Any()) {
      // put more fundamental actions later
      actions.Reverse();
      foreach (var action in actions) {
        var actionButton = Instantiate(itemActionButtonPrefab, new Vector3(), Quaternion.identity, actionsContainer.transform);
        actionButton.GetComponentInChildren<TMPro.TMP_Text>().text = action.displayName;
        actionButton.GetComponent<Button>().onClick.AddListener(() => {
          player.action = action;
          Close();
          CloseInventory();
        });
      }
    } else {
      actionsContainer.SetActive(false);
    }

    Instantiate(spriteBase, spriteContainer.GetComponent<RectTransform>().position, Quaternion.identity, spriteContainer.transform);

    stats.text = item.GetStats();

    flavorText.text = ItemInfo.GetFlavorTextForItem(item);
  }

  // Update is called once per frame
  void Update() {
    var text = item.GetStats();
    if (item is IDurable d) {
      text += $"\nDurability: {d.durability}/{d.maxDurability}.";
    }
    if (item is IWeapon w) {
      var (min, max) = w.AttackSpread;
      text += $"\n{min} - {max} damage.";
    }
    stats.text = text.Trim();
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
