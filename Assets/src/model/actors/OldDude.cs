using System;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "An old dude. Looks like he's seen his way around the caves a few times.")]
public class OldDude : AIActor {
  public OldDude(Vector2Int pos) : base(pos) {
    faction = Faction.Neutral;
    hp = baseMaxHp = 8;
    SetTasks(new MoveRandomlyTask(this));
  }

  protected override ActorTask GetNextTask() {
        return new MoveRandomlyTask(this);
  }
}