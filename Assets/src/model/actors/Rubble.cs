using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Destructible. Blocks vision.")]
public class Rubble : Body, IBlocksVision, IAnyDamageTakenModifier {
  public Rubble(Vector2Int pos, int hp = 1) : base(pos) {
    this.hp = this.baseMaxHp = hp;
  }

  public int Modify(int input) {
    return 1;
  }
}

[System.Serializable]
[ObjectInfo(description: "Destructible.")]
public class Stump : Body, IAnyDamageTakenModifier {
  public Stump(Vector2Int pos) : base(pos) {
    this.hp = this.baseMaxHp = 3;
  }

  public int Modify(int input) {
    return 1;
  }
}

/// Note - not implemented on moving entities yet
public interface IBlocksVision { }