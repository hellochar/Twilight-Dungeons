using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Updates the UI to match any Item slot having an item or being empty.
/// Is responsible for Instantiating the Item gameObject.
public abstract class ItemSlotController : MonoBehaviour {
  protected GameObject itemChild;
  public abstract Item item { get; }
  [NonSerialized]
  public Item activeItem;

  public virtual void Update() {
    if (GameModel.main == null) {
      gameObject.SetActive(false);
      return;
    }
    /// TODO this won't update properly if the item is swapped
    if (item == null && itemChild != null) {
      UpdateUnused();
    } else if (item != null) {
      if (itemChild == null) {
        itemChild = UpdateInUse(item);
        activeItem = item;
      } else if (activeItem != item) {
        // if the item has been swapped
        UpdateUnused();
        itemChild = UpdateInUse(item);
        activeItem = item;
      }
    }
  }

  protected virtual void UpdateUnused() {
    Destroy(itemChild);
    itemChild = null;
    activeItem = null;
  }

  protected abstract GameObject UpdateInUse(Item item);
}
