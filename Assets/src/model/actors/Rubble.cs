using System;
using UnityEngine;

/// signals that attacking this doesn't use item durability
[Serializable]
public class Destructible : Body, IAnyDamageTakenModifier, IHideInSidebar {
  public Destructible(Vector2Int pos, int hp) : base(pos) {
    this.hp = this.baseMaxHp = hp;
  }

  public Destructible(Vector2Int pos) : this(pos, 1) { }

  public int Modify(int input) {
    return 1;
  }
}

[Serializable]
[ObjectInfo(description: "Destructible. Blocks vision.")]
public class Rubble : Destructible, IBlocksVision {
  public Rubble(Vector2Int pos) : base(pos, 1) {}
}

[System.Serializable]
[ObjectInfo(description: "Destructible.")]
public class Stump : Destructible {
  public Stump(Vector2Int pos) : base(pos, 3) {}
}

/// Note - not implemented on moving entities yet
public interface IBlocksVision { }
public interface IHideInSidebar { }