using System;
using UnityEngine;

/// <summary>
/// Homebase state entity for The Stranger recurring NPC.
/// Placed on the home floor when the player helps the Stranger.
/// Future Stranger events check for this entity and branch on timesHelped.
/// </summary>
[Serializable]
[ObjectInfo("colored_transparent_packed_1046",
  description: "A figure in a tattered cloak, resting by your garden.",
  flavorText: "They seem at ease here.")]
public class StrangerNPC : Body, IHideInSidebar {
  public int timesHelped = 0;

  public StrangerNPC(Vector2Int pos) : base(pos) {
    this.hp = this.baseMaxHp = 100;
  }

  public override string displayName => "The Stranger";
}
