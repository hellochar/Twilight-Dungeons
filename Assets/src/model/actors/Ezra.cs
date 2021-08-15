using System;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Your wife! What's happened to her?")]
public class Ezra : Actor, IAnyDamageTakenModifier {
  public Ezra(Vector2Int pos) : base(pos) {
    faction = Faction.Ally;
    hp = baseMaxHp = 24;
    SetTasks(new SleepTask(this, null, true));
  }

  public int Modify(int input) {
    return 0;
  }
}

[Serializable]
public class TreeOfLife : Tile {
  public TreeOfLife(Vector2Int pos) : base(pos) {
  }
  public override float BasePathfindingWeight() => 0;
  public override bool ObstructsVision() => false;
}