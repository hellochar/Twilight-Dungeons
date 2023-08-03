using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MercenaryController : ActorController {
  public Mercenary mercenary => (Mercenary) actor;
  public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    // oldDude.RevealFloor();
    if (mercenary.isHired) {
      return base.GetPlayerInteraction(pointerEventData);
    } else {
      return new ArbitraryPlayerInteraction(() => {
        var buttons = new List<(string, Action)>();
        buttons.Add(("Hire (100 water)", mercenary.Hire));
        Popups.CreateStandard("Mercenary", "", "\"You look a little out of your league partner. I'll help you, for a price.\"", "", null, buttons);
      });
    }
  }
}