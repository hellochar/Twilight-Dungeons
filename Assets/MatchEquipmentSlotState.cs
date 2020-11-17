using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatchEquipmentSlotState : MatchItemSlotState {
  public EquipmentSlot slot;
  private Equipment equipment;
  public GameObject placeholder;
  public override Item item => equipment[(int) slot];

  public override void Start() {
    equipment = GameModel.main.player.equipment;
    placeholder = transform.Find("Placeholder").gameObject;
    base.Start();
  }
}
