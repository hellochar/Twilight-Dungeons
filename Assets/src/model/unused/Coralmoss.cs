using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[System.Serializable]
class Coralmoss : Grass, ISteppable {
  public static bool CanOccupy(Tile tile) => tile is Ground && !(tile.body is Coral);
  public Coralmoss(Vector2Int pos) : base(pos) {
    this.timeNextAction = timeCreated + 1;
  }

  bool bTriedGrowing = false;

  public float Step() {
    if (!bTriedGrowing) {
      bTriedGrowing = true;
      // grow towards your horizontal neighbors
      var neighborsToGrow = new List<Tile>() { floor.tiles[pos + Vector2Int.left], floor.tiles[pos + Vector2Int.right] }
        .Where(t => CanOccupy(t) && t.CanBeOccupied() && !(t.grass is Coralmoss));
      foreach (var n in neighborsToGrow) {
        floor.Put(new Coralmoss(n.pos));
      }
      return 5;
    }
    GameModel.main.EnqueueEvent(() => {
      if (tile.CanBeOccupied()) {
        floor.Put(new Coral(pos));
      }
      KillSelf();
    });
    return 1;
  }

  public float timeNextAction { get; set; }
  public float turnPriority => 50;
}

[System.Serializable]
internal class Coral : Destructible {
  public Coral(Vector2Int pos) : base(pos, 1) {
  }
}

[System.Serializable]

[ObjectInfo("coralmoss", "rough to the touch", "Places a Coral below you that grows horizontally")]
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
    stacks--;
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