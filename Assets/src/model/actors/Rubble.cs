using System;
using System.Linq;
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
public class Stump : Destructible, IBlocksVision {
  public Stump(Vector2Int pos) : base(pos, 3) {}
}

[System.Serializable]
[ObjectInfo(description: "Blocks vision.")]
public class Stalk : Destructible, IBlocksVision, IDeathHandler {

  public Stalk(Vector2Int pos) : base(pos) {}

  // kill all stalks on the map
  public void HandleDeath(Entity source) {
    if (!(source is Stalk)) {
      foreach(var stalk in floor.bodies.OfType<Stalk>().ToArray()) {
        if (stalk != this) {
          stalk.Kill(this);
        }
      }
    }
  }
}

/// Note - not implemented on moving entities yet
public interface IBlocksVision { }
public interface IBlocksExploration : IBlocksVision { }
public interface IHideInSidebar { }