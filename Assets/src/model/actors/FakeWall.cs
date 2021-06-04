using System;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Blocks vision and movement.", flavorText: "Hard earth that has weathered centuries of erosion; it's not going anywhere.")]
public class FakeWall : Body, IBlocksVision, IAnyDamageTakenModifier, IDeathHandler {
  public override string displayName => "Wall";
  public FakeWall(Vector2Int pos, int hp = 3) : base(pos) {
    this.hp = this.baseMaxHp = hp;
  }

  public int Modify(int input) {
    return 1;
  }

  public void HandleDeath(Entity source) {
    floor.Put(new Hole(pos));
  }
}

[Serializable]
[ObjectInfo(description: "Weakened stone has collapsed here, opening up a previously sealed off pathway. But why was it sealed off...?")]
public class Hole : Tile, IActorEnterHandler {
  public Hole(Vector2Int pos) : base(pos) {
  }

  public void HandleActorEnter(Actor who) {
    if (who is Player) {
      // transport player to boss room
      Debug.Log("transport player!");
    }
  }
}
