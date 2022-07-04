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

[System.Serializable]
[ObjectInfo(description: "Grows upwards.")]
public class Stalk : Destructible, ISteppable {

  public float timeNextAction { get; set; }

  public float turnPriority => 9;

  public Stalk(Vector2Int pos) : base(pos) {
    timeNextAction = timeCreated + 5;
  }

  public float Step() {
    var up = floor.tiles[new Vector2Int(pos.x, pos.y + 1)];
    if (up.CanBeOccupied() && !(up.body is Stalk)) {
      floor.Put(new Stalk(up.pos));
    }
    return 5;
  }
}

/// Note - not implemented on moving entities yet
public interface IBlocksVision { }
public interface IHideInSidebar { }