using UnityEngine;

public class TeleportStone : Actor {
  public TeleportStone(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 999;
    faction = Faction.Neutral;
    this.timeNextAction += 99999;
  }

  protected override float Step() {
    return 99999;
  }
}
