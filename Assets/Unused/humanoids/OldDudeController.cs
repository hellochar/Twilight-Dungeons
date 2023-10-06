using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OldDudeController : NPCController {
  public OldDude oldDude => (OldDude) npc;
  public GameObject deathbloom;

  public override void Update() {
    base.Update();
    deathbloom?.SetActive(oldDude.questCompleted);
  }
}