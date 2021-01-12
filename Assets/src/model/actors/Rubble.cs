using UnityEngine;

public class Rubble : Body, IBlocksVision, IAnyDamageTakenModifier {
  public Rubble(Vector2Int pos, int hp = 1) : base(pos) {
    this.hp = this.baseMaxHp = hp;
  }

  public int Modify(int input) {
    return 1;
  }
}

public class Stump : Body, IAnyDamageTakenModifier {
  public Stump(Vector2Int pos) : base(pos) {
    this.hp = this.baseMaxHp = 1;
  }

  public int Modify(int input) {
    return 1;
  }
}

/// Note - not implemented on moving entities yet
public interface IBlocksVision { }