using System;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Your wife! What's happened to her?")]
public class Ezra : Actor {
  public Ezra(Vector2Int pos) : base(pos) {
    faction = Faction.Ally;
    hp = baseMaxHp = 16;
    SetTasks(new SleepTask(this, null, true));
  }
}