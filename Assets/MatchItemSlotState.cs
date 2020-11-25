using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class MatchItemSlotState : MonoBehaviour {
  private Shadow shadow;
  /// the ragged border/fill representing the slot
  private Image image;
  protected GameObject itemChild;
  protected GameObject itemPrefab;
  public abstract Item item { get; }

  public virtual void Start() {
    itemPrefab = Resources.Load<GameObject>("UI/Item");
    shadow = GetComponent<Shadow>();
    image = GetComponent<Image>();
    itemChild = transform.Find("Item")?.gameObject ?? transform.Find("ItemOnPlayer")?.gameObject;
  }

  public virtual void Update() {
    /// TODO this won't update properly if the item is swapped
    if (item == null && itemChild != null) {
      UpdateUnused();
    } else if (item != null) {
      if (itemChild == null) {
        UpdateInUse(item);
      } else if (itemChild.GetComponent<MatchItemState>().item != item) {
        // if the item has been swapped
        UpdateUnused();
        UpdateInUse(item);
      }
    }
  }

  protected virtual void UpdateUnused() {
    shadow.enabled = false;
    image.fillCenter = false;
    image.color = UnusedColor;

    Destroy(itemChild);
    itemChild = null;
  }

  protected virtual void UpdateInUse(Item item) {
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
