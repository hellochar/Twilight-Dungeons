using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// Responsible for: creating and destroying slots
public class InventoryController : MonoBehaviour {
  public Inventory inventory;
  public bool trimExcess = false;

  void Start() {
    MatchNumberOfSlots();
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
