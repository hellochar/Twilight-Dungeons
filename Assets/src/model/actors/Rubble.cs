using UnityEngine;

public class Rubble : Actor, IBlocksVision {
  public Rubble(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 7;
    faction = Faction.Neutral;
    this.timeNextAction += 99999;
  }

  protected override float Step() {
    return 99999;
  }
}

/// Note - not implemented on moving entities yet
public interface IBlocksVision { }