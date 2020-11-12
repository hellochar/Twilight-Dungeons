using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatchItemSlotState : MonoBehaviour {
  int slotIndex;
  private Inventory inventory;
  private Shadow shadow;
  private Image image;
  private GameObject itemChild;
  private GameObject itemPrefab;

  void Start() {
    inventory = GameModel.main.player.inventory;
    slotIndex = transform.GetSiblingIndex();
    itemPrefab = Resources.Load<GameObject>("UI/Item");
    shadow = GetComponent<Shadow>();
    image = GetComponent<Image>();
    itemChild = transform.Find("Item")?.gameObject;
  }

  void Update() {
    Item item = inventory[slotIndex];
    if (item == null && itemChild != null) {
      UpdateUnused();
    } else if (item != null && itemChild == null) {
      UpdateInUse(item);
    }
  }

  private void UpdateUnused() {
    shadow.enabled = false;
    image.fillCenter = false;
    image.color = UnusedColor;

    Destroy(itemChild);
    itemChild = null;
  }

  private void UpdateInUse(Item item) {
    shadow.enabled = true;
    image.fillCenter = true;
    image.color = InUseColor;

    itemChild = Instantiate(itemPrefab, new Vector3(), Quaternion.identity, this.transform);
    itemChild.transform.localPosition = new Vector3(0, 0, 0);
    itemChild.GetComponent<MatchItemState>().item = item;
  }

  public static Color UnusedColor = new Color(0.1882353f, 0.2039216f, 0.227451f, 0.2470588f);
  public static Color InUseColor = new Color(0.1882353f, 0.2039216f, 0.227451f, 1f);

}
