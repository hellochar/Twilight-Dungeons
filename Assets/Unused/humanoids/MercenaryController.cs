using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MercenaryController : ActorController {
  public Mercenary mercenary => (Mercenary) actor;
  public override void HandleInteracted(PointerEventData pointerEventData) {
    // oldDude.RevealFloor();
    if (mercenary.isHired) {
      base.HandleInteracted(pointerEventData);
    } else {
      var buttons = new List<(string, Action)>();
      buttons.Add(("Hire (100 water)", mercenary.Hire));
      Popups.Create("Mercenary", "", "\"You look a little out of your league partner. I'll help you, for a price.\"", "", null, null, buttons);
    }
  }
}