using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatchEquipmentSlotState : MatchItemSlotState {
  public EquipmentSlot slot;
  private Equipment equipment;
  public GameObject placeholder;
  public override Item item => equipment[slot];

  public override void Start() {
    equipment = GameModel.main.player.equipment;
    placeholder = transform.Find("Placeholder")?.gameObject;
    base.Start();
  }

  protected override void UpdateUnused() {
    placeholder?.SetActive(true);
    base.UpdateUnused();
  }

  protected override void UpdateInUse(Item item) {
    placeholder?.SetActive(false);
    base.UpdateInUse(item);
  }
}
