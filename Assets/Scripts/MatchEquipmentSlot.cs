using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatchEquipmentSlot : MatchInventorySlot {
  public EquipmentSlot slot;
  public GameObject placeholder;

  public override void Start() {
    base.Start();
    slotIndex = (int) slot;
    inventory = GameModel.main.player.equipment;
    placeholder = transform.Find("Placeholder")?.gameObject;
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
