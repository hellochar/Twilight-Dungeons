using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[System.Serializable]
class Coralmoss : Grass, ISteppable {
  public static bool CanOccupy(Tile tile) => tile is Ground && !(tile.body is Coral);
  public Coralmoss(Vector2Int pos) : base(pos) {
    this.timeNextAction = timeCreated + 8;
  }

  public float Step() {
    // look at adjacent squares - if any of them have 3 coralmoss neighbors, grow coralmoss onto it
    var neighborsToGrow = floor.GetAdjacentTiles(pos).Where((t) => Coralmoss.CanOccupy(t) && !(t.grass is Coralmoss) && NumCoralmossNeighbors(t) == 3);
    if (neighborsToGrow.Any()) {
      OnNoteworthyAction();
      foreach (var n in neighborsToGrow) {
        floor.Put(new Coralmoss(n.pos));
      }
    }
    if (age > 8) {
      GameModel.main.EnqueueEvent(() => {
        floor.Put(new Coral(pos));
        Kill();
      });
    }
    return 8;
  }

  public int NumCoralmossNeighbors(Tile tile) {
    return tile.floor.GetAdjacentTiles(tile.pos).Where((t) => t.grass is Coralmoss || t.body is Coral).Count();
  }

  public float timeNextAction { get; set; }
  public float turnPriority => 50;
}

[System.Serializable]
internal class Coral : Body, IAnyDamageTakenModifier, IDeathHandler {
  public Coral(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 3;
  }

  public void HandleDeath() {
    var floor = this.floor;
    var item = new ItemOnGround(pos, new ItemCoralChunk(1), pos);
    GameModel.main.EnqueueEvent(() => {
      floor.Put(item);
    });
  }

  public int Modify(int input) {
    return 1;
  }
}

[System.Serializable]

[ObjectInfo("coral", "rough to the touch")]
internal class ItemCoralChunk : Item, IStackable {
  public ItemCoralChunk(int stacks) {
    this.stacks = stacks;
  }
  public int stacksMax => 100;

  private int _stacks;
  public int stacks {
    get => _stacks;
    set {
      if (value < 0) {
        throw new System.ArgumentException("Setting negative stack!" + this + " to " + value);
      }
      _stacks = value;
      if (_stacks == 0) {
        Destroy();
      }
    }
  }

  public void Plant(Actor a) {
    a.floor.Put(new Coralmoss(a.pos));
  }

  public override List<MethodInfo> GetAvailableMethods(Player player) {
    var m = base.GetAvailableMethods(player);
    m.Add(GetType().GetMethod("Plant"));
    return m;
  }

  internal override string GetStats() {
    return "Plant this coral chunk to grow Coralmoss where you're standing.";
  }
}