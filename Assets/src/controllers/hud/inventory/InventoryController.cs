using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// Responsible for: creating and destroying slots
public class InventoryController : MonoBehaviour {
  [NonSerialized]
  public Inventory inventory;
  public bool trimExcess = false;

  public virtual void Start() {
    if (inventory == null) {
      inventory = GameModel.main.player.inventory;
    }
    MatchNumberOfSlots();
  }

  public InventorySlotController GetSlot(Item item) {
    var index = inventory.IndexOf(item);
    if (index != -1) {
      return GetSlot(index);
    }
    return null;
  }

  public InventorySlotController GetSlot(int index) {
    return transform.GetChild(index)?.GetComponent<InventorySlotController>();
  }

  void MatchNumberOfSlots() {
    var capacity = trimExcess ? inventory.ItemsNonNull().Count() : inventory.capacity;
    // e.g. 5 children, 4 hearts
    for (int i = transform.childCount; i > capacity; i--) {
      Destroy(transform.GetChild(i - 1).gameObject);
    }

    for (int i = transform.childCount; i < capacity; i++) {
      Instantiate(PrefabCache.UI.GetPrefabFor("Slot"), transform);
    }
  }
}
