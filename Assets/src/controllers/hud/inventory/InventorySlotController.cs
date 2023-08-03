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
  public bool allowDragAndDrop => inventoryController?.allowDragAndDrop ?? false;
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
    if (allowDragAndDrop) {
      OnBeginDrag += HandleBeginDrag;
      OnEndDrag += HandleEndDrag;
    }
  }

  void OnDestroy() {
    OnBeginDrag -= HandleBeginDrag;
    OnEndDrag -= HandleEndDrag;
  }

  private void HandleBeginDrag(Item obj) {
    // image.fillCenter = true;
    image.color = DropTargetColor;
  }

  private void HandleEndDrag(Item obj) {
    // image.fillCenter = item == null ? false : true;
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

  public void OnDrop(PointerEventData eventData) {
    if (allowDragAndDrop) {
      var itemController = eventData.selectedObject.GetComponent<ItemController>();
      var item = itemController.item;

      // Debug.Log("OnDrop " + item + " onto slot " + slotIndex, this);
      inventory.AddItem(item, slotIndex);
    }
  }

  public static Color UnusedColor = new Color(0.1882353f, 0.2039216f, 0.227451f, 0.2470588f);
  public static Color InUseColor = new Color(0.1882353f, 0.2039216f, 0.227451f, 1f);
  public static Color DropTargetColor = new Color(0.990566f, 0.990566f, 0.2186721f, 1f);
}
