using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MossManController : ActorController {
  public MossMan mossMan => (MossMan) actor;
  public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    return new ArbitraryPlayerInteraction(() => {
      Popups.CreateStandard("MossMan", "", mossMan.TestQuestStatus(), "", null);
    });
  }
}