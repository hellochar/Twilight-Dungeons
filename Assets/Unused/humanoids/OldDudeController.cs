using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OldDudeController : ActorController {
  public OldDude oldDude => (OldDude) actor;
  public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    return new ArbitraryPlayerInteraction(() => {
      Popups.Create("Old Dude", "", oldDude.TestQuestStatus(), "", null);
    });
  }
}