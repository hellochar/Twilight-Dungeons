using System;
using UnityEngine;

[Serializable]
public class OrganicMatterOnGround : ItemOnGround, IActorEnterHandler {
  // just to capture the ObjectInfo
  [ObjectInfo("plant-matter", description: "Turn into organic matter at home.")]
  private class ItemOrganicMatterProxy : Item {}

  static ItemOrganicMatterProxy proxy = new ItemOrganicMatterProxy();

  public OrganicMatterOnGround(Vector2Int pos, Vector2Int? start = null) : base(pos, proxy, start) { }

  public override void PickUp() {
    if (!IsDead) {
      // base.PickUp();
      GameModel.main.player.organicMatter++;
      KillSelf();
    }
  }

  public void HandleActorEnter(Actor who) {
    PickUp();
  }

  public override void StepDay() {
    // do not decompose
    // base.StepDay();
  }
}