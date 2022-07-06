using System;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo("llaora", flavorText: "", description: "You may Disperse the Llaora, confusing Enemies in radius 2 for 10 turns.")]
public class Llaora : Grass {
  public static bool CanOccupy(Tile tile) => tile is Ground;

  public Llaora(Vector2Int pos) : base(pos) { }

  public float radius => 2.5f;

  public void Disperse(Player who) {
    OnNoteworthyAction();
    foreach (var body in floor.EnumerateCircle(pos, radius).Select(t => floor.bodies[t])) {
      if (body is Actor actor && actor.faction == Faction.Enemy) {
        actor.statuses.Add(new ConfusedStatus(10));
      }
    }
    Kill(who);
  }
}
