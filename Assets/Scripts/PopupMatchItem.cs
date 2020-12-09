﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PopupMatchItem : MonoBehaviour {
  public Item item;
  TMPro.TMP_Text stats;

  void Start() {
    stats = transform.Find("Frame/Stats").GetComponent<TMPro.TMP_Text>();
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
    var inventoryContainer = GameObject.Find("Inventory Container");
    inventoryContainer.SetActive(false);
  }
}
