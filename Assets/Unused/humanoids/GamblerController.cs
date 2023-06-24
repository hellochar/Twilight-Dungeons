using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GamblerController : ActorController {
  public Gambler gambler => (Gambler) actor;
  public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    return new ArbitraryPlayerInteraction(() => {
      var buttons = new List<(string, Action)>();
      buttons.Add(("Gamble (50 water)", gambler.Gamble));
      Popups.Create("Gambler", "", "The Gambler reveals lots of water.\n\"I can make you rich! Won't you take the plunge?\"", "", null, null, buttons);
    });
  }
}