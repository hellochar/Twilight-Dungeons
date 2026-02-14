using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotController : ItemSlotController, IDropHandler {
  public static event Action<Item> OnBeginDrag;
  public static event Action<Item> OnEndDrag;
  public static void BeginDragging(Item item) {
    OnBeginDrag?.Invoke(item);
  }

  public static void EndDragging(Item item) {
    OnEndDrag?.Invoke(item);
  }

  private GameObject itemPrefab;
  private Shadow shadow;
  /// the ragged border/fill representing the slot
  private Image image;
  public int slotIndex;
  [NonSerialized]
  protected Inventory inventory;
  public InventoryController inventoryController;
  public bool allowDragAndDrop => inventory?.allowDragAndDrop ?? false;
  public override Item item => inventory[slotIndex];

  public virtual void Start() {
    itemPrefab = PrefabCache.UI.GetPrefabFor("Item");
    shadow = GetComponent<Shadow>();
    image = GetComponent<Image>();
    slotIndex = transform.GetSiblingIndex();
    if (inventoryController == null) {
      inventoryController = GetComponentInParent<InventoryController>();
    }
    inventory = inventoryController?.inventory ?? GameModel.main.player.inventory;
    itemChild = transform.Find("Item")?.gameObject;
    OnBeginDrag += HandleBeginDrag;
    OnEndDrag += HandleEndDrag;
  }

  void OnDestroy() {
    OnBeginDrag -= HandleBeginDrag;
    OnEndDrag -= HandleEndDrag;
  }

  /// <summary>
  /// Returns true if this slot can accept the given item as a drop target.
  /// </summary>
  public virtual bool CanAcceptItem(Item draggedItem) {
    if (draggedItem == null) return false;
    // Don't allow dropping onto the slot the item is already in
    if (draggedItem.inventory == inventory && inventory.IndexOf(draggedItem) == slotIndex) {
      return false;
    }

    // If the dragged item comes from equipment and this slot has an item,
    // the swap would try to put our item into the equipment slot.
    // Only allow if our item is equippable for that slot.
    var existingItem = item;
    if (draggedItem.inventory is Equipment && existingItem != null) {
      if (draggedItem is EquippableItem draggedEquippable &&
          existingItem is EquippableItem eq && eq.slot == draggedEquippable.slot) {
        return true;
      }
      // If existing item can't go into equipment, reject the swap
      return false;
    }

    return true;
  }

  private void HandleBeginDrag(Item draggedItem) {
    if (CanAcceptItem(draggedItem)) {
      image.color = DropTargetColor;
    } else {
      image.color = InvalidDropTargetColor;
    }
  }

  private void HandleEndDrag(Item obj) {
    image.color = item == null ? UnusedColor : InUseColor;
  }

  protected override void UpdateUnused() {
    shadow.enabled = false;
    image.fillCenter = false;
    image.color = UnusedColor;
    base.UpdateUnused();
  }

  protected override GameObject UpdateInUse(Item item) {
    shadow.enabled = true;
    image.fillCenter = true;
    image.color = InUseColor;

    var child = Instantiate(itemPrefab, new Vector3(), Quaternion.identity, this.transform);
    child.transform.localPosition = new Vector3(0, 0, 0);
    child.GetComponent<ItemController>().item = item;
    return child;
  }

  public virtual void OnDrop(PointerEventData eventData) {
    if (!allowDragAndDrop) return;
    var itemController = eventData.selectedObject.GetComponent<ItemController>();
    if (itemController == null) return;
    var draggedItem = itemController.item;

    if (!CanAcceptItem(draggedItem)) return;

    inventory.AddItem(draggedItem, slotIndex);
  }

  public static Color UnusedColor = new Color(0.1882353f, 0.2039216f, 0.227451f, 0.2470588f);
  public static Color InUseColor = new Color(0.1882353f, 0.2039216f, 0.227451f, 1f);
  public static Color DropTargetColor = new Color(0.990566f, 0.990566f, 0.2186721f, 1f);
  public static Color InvalidDropTargetColor = new Color(0.5f, 0.2f, 0.2f, 0.5f);
}
