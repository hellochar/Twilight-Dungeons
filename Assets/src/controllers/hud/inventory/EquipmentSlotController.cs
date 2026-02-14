using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlotController : InventorySlotController {
  public EquipmentSlot slot;
  public GameObject placeholder;

  public override void Start() {
    base.Start();
    slotIndex = (int) slot;
    inventory = GameModel.main.player.equipment;
    placeholder = transform.Find("Placeholder")?.gameObject;
  }

  public override bool CanAcceptItem(Item draggedItem) {
    if (draggedItem == null) return false;
    // Only EquippableItems matching this slot can be dropped here
    if (!(draggedItem is EquippableItem equippable)) return false;
    if (equippable.slot != slot) return false;
    // Don't allow if the item is already in this slot
    if (draggedItem.inventory == inventory && inventory.IndexOf(draggedItem) == slotIndex) return false;
    // Don't allow if a sticky item occupies this slot
    if (inventory[slotIndex] is ISticky) return false;
    return true;
  }

  public override void OnDrop(PointerEventData eventData) {
    if (!allowDragAndDrop) return;
    var itemController = eventData.selectedObject.GetComponent<ItemController>();
    if (itemController == null) return;
    var draggedItem = itemController.item;

    if (!CanAcceptItem(draggedItem)) return;

    // Use Equipment.AddItem(item) which handles slot routing and equip hooks
    var equipment = inventory as Equipment;
    equipment.AddItem(draggedItem);
  }

  protected override void UpdateUnused() {
    placeholder?.SetActive(true);
    base.UpdateUnused();
  }

  protected override GameObject UpdateInUse(Item item) {
    placeholder?.SetActive(false);
    return base.UpdateInUse(item);
  }
}
