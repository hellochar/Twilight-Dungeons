using UnityEngine;

public class Rubble : Actor, IBlocksVision, IAnyDamageTakenModifier {
  public Rubble(Vector2Int pos, int hp = 1) : base(pos) {
    this.hp = this.baseMaxHp = hp;
    faction = Faction.Neutral;
    this.timeNextAction += 99999;
  }

  public int Modify(int input) {
    return 1;
  }

  public override float Step() {
    return 99999;
  }
}

public class Stump : Actor {
  public Stump(Vector2Int pos) : base(pos) {
    timeNextAction += 99999;
  }

  public override float Step() {
    return 99999;
  }
}

/// Note - not implemented on moving entities yet
public interface IBlocksVision { }