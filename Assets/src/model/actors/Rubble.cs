using UnityEngine;

public class Rubble : Actor, IBlocksVision {
  public Rubble(Vector2Int pos) : base(pos) {
    hp = hpMax = 8;
    faction = Faction.Neutral;
    this.timeNextAction += 99999;
  }

  protected override float Step() {
    return 99999;
  }
}

/// Note - not implemented on moving entities yet
public interface IBlocksVision { }