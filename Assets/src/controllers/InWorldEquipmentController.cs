using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InWorldEquipmentController : ItemSlotController {
  private GameObject itemPrefab;

  public EquipmentSlot slot;
  public override Item item => GameModel.main.player.equipment[slot];

  void Start() {
    itemPrefab = Resources.Load<GameObject>("UI/ItemOnPlayer");
    itemChild = transform.Find("ItemOnPlayer")?.gameObject;
  }

  protected override GameObject UpdateInUse(Item item) {
    var showOnPlayer = ObjectInfo.InfoFor(item).showOnPlayer;
    if (showOnPlayer) {
      var child = Instantiate(itemPrefab, new Vector3(), Quaternion.identity, this.transform);
      child.transform.localPosition = new Vector3(0, 0, 0);
      child.GetComponent<SpriteRenderer>().sprite = ObjectInfo.GetSpriteFor(item);
      return child;
    }
    return null;
  }
}
