using System;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotController : ItemSlotController {
  private GameObject itemPrefab;
  private Shadow shadow;
  /// the ragged border/fill representing the slot
  private Image image;
  public int slotIndex;
  [NonSerialized]
  protected Inventory inventory;
  public override Item item => inventory[slotIndex];

  public virtual void Start() {
    if (GameModel.main?.player?.inventory == null) {
      Destroy(gameObject);
      return;
    }
    itemPrefab = PrefabCache.UI.GetPrefabFor("Item");
    shadow = GetComponent<Shadow>();
    image = GetComponent<Image>();
    slotIndex = transform.GetSiblingIndex();
    inventory = GetComponentInParent<InventoryController>()?.inventory ?? GameModel.main.player.inventory;
    itemChild = transform.Find("Item")?.gameObject;
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

  public static Color UnusedColor = new Color(0.1882353f, 0.2039216f, 0.227451f, 0.2470588f);
  public static Color InUseColor = new Color(0.1882353f, 0.2039216f, 0.227451f, 1f);
}
