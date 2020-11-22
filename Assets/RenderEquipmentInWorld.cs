using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// similar to MatchEquipmentSlot except it doesn't do the image/shadow stuff
public class RenderEquipmentInWorld : MatchEquipmentSlotState {
  public override void Start() {
    base.Start();
    itemPrefab = Resources.Load<GameObject>("UI/ItemOnPlayer");
  }

  protected override void UpdateUnused() {
    Destroy(itemChild);
    itemChild = null;
  }

  /// TODO this is copy/pasted of part of MatchItemSlotState. Eventually 
  /// refactor this logic into its own location (move image/shadow out to a separate level)
  protected override void UpdateInUse(Item item) {
    var showOnPlayer = ItemInfo.InfoFor(item).showOnPlayer;
    if (showOnPlayer) {
      itemChild = Instantiate(itemPrefab, new Vector3(), Quaternion.identity, this.transform);
      itemChild.transform.localPosition = new Vector3(0, 0, 0);
      itemChild.GetComponent<SpriteRenderer>().sprite = ItemInfo.GetSpriteForItem(item);
    }
  }
}
