using UnityEngine;

public class Rubble : Actor, IBlocksVision, IAnyDamageTakenModifier {
  public Rubble(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 3;
    faction = Faction.Neutral;
    this.timeNextAction += 99999;
  }

  public int Modify(int input) {
    return 1;
  }

  protected override float Step() {
    return 99999;
  }
}

/// Note - not implemented on moving entities yet
public interface IBlocksVision { }