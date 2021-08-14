using System;
using UnityEngine;

[Serializable]
[ObjectInfo("redcap", flavorText: "", description: "You may pop the Redcap, applying the Vulnerable Status to adjacent enemies for 7 turns.\nVulnerable creatures take +1 attack damage.")]
public class Redcap : Grass {
  public Redcap(Vector2Int pos) : base(pos) { }

  public void Pop(Player who) {
    OnNoteworthyAction();
    foreach (var p in floor.AdjacentActors(pos)) {
      if (p.faction == Faction.Enemy) {
        p.statuses.Add(new VulnerableStatus(7));
      }
    }
    Kill(who);
  }
}
