using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OldDudeController : ActorController {
  public OldDude oldDude => (OldDude) actor;
  public override void HandleInteracted(PointerEventData pointerEventData) {
    oldDude.RevealFloor();
  }

}