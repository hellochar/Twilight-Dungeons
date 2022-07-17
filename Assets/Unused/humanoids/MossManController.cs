using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MossManController : ActorController {
  public MossMan mossMan => (MossMan) actor;
  public override void HandleInteracted(PointerEventData pointerEventData) {
    // oldDude.RevealFloor();
    Popups.Create("MossMan", "", mossMan.TestQuestStatus(), "", null);
  }
}