using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Does not attack.\nLeaves an explosive corpse on death.", flavorText: "How such a creature was able to survive and breed is Nature's mystery.")]
public class Boombug : AIActor {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.WAIT] = 2f,
    [ActionType.MOVE] = 2f,
  };
  protected override ActionCosts actionCosts => Boombug.StaticActionCosts;
  public Boombug(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 1;
    faction = Faction.Neutral;
  }

  protected override ActorTask GetNextTask() {
    if (MyRandom.value < 0.5f) {
      return new WaitTask(this, MyRandom.Range(1, 5));
    } else {
      var range5Tiles = floor.EnumerateCircle(pos, 5).Where((pos) => floor.tiles[pos].CanBeOccupied());
      var target = Util.RandomPick(range5Tiles);
      return new MoveToTargetTask(this, target);
    }
  }

  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);
    var floor = this.floor;
    // leave a corpse on death
    // explode and hurt everything nearby
    GameModel.main.EnqueueEvent(() => floor.Put(new BoombugCorpse(pos)));
  }
}

[Serializable]
[ObjectInfo(spriteName: "boombug", flavorText: "This boombug corpse has been defused, but can be easily triggered again...")]
public class ItemBoombugCorpse : Item, IStackable, ITargetedAction<Tile> {
  public ItemBoombugCorpse(int stacks) {
    this.stacks = stacks;
  }
  public int stacksMax => 7;

  private int _stacks;
  public int stacks {
    get => _stacks;
    set {
      if (value < 0) {
        throw new ArgumentException("Setting negative stack!" + this + " to " + value);
      }
      _stacks = value;
      if (_stacks == 0) {
        Destroy();
      }
    }
  }

  internal override string GetStats() => "Throw at any visible tile to leave an explosive Boombug Corpse there.";

  public void Throw(Player player, Vector2Int position) {
    player.floor.Put(new BoombugCorpse(position));
    stacks--;
  }

  public string TargettedActionName => "Throw";
  public string TargettedActionDescription => "Choose where to throw Boombug.";
  public IEnumerable<Tile> Targets(Player player) =>
    player.floor
      .EnumerateFloor()
      .Select(p => player.floor.tiles[p])
      .Where((p) => p.CanBeOccupied() && p.visibility == TileVisiblity.Visible);

  public void PerformTargettedAction(Player player, Entity target) {
    player.task = new GenericPlayerTask(player, () => Throw(player, target.pos));
  }
}

[Serializable]
[ObjectInfo("Explodes after one turn, dealing 3 damage and destroying unprotected Grasses in adjacent squares.\nAttacking it will defuse the corpse.")]
public class BoombugCorpse : Actor, IDeathHandler {
  public override float turnPriority => 30;
  private bool exploded = false;
  [field: NonSerialized] /// controller only
  public event Action OnExploded;
  public BoombugCorpse(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 1;
    timeNextAction += 1;
    SetTasks(new ExplodeTask(this));
  }

  public void HandleDeath(Entity source) {
    if (!exploded) {
      // We died before we could explode! Leave a corpse item instead.
      var inventory = new Inventory(new ItemBoombugCorpse(1));
      var floor = this.floor;
      var pos = this.pos;
      GameModel.main.EnqueueEvent(() => inventory.TryDropAllItems(floor, pos));
    }
  }

  public override float Step() {
    Explode();
    return baseActionCost;
  }

  void Explode() {
    if (exploded) {
      return;
    }
    exploded = true;
    OnExploded?.Invoke();
    var adjacentBoombugs = new List<BoombugCorpse>();
    foreach (var tile in floor.GetAdjacentTiles(pos)) {
      if (tile.body is BoombugCorpse c && c != this) {
        adjacentBoombugs.Add(c);
      } else if (tile.body != null && tile.body != this) {
        this.Attack(tile.body);
      }
      if ((tile.body == null || tile == this.tile) && tile.grass != null) {
        tile.grass.Kill(this);
      }
    }
    Kill(this);
    GameModel.main.EnqueueEvent(() => {
      foreach (var b in adjacentBoombugs) {
        b.Explode();
      }
    });
  }

  internal override (int, int) BaseAttackDamage() {
    return (3, 3);
  }
}

/// this doesn't contain gameplay logic; it's just to signal creating an ExplodeTask visual marker
[System.Serializable]
public class ExplodeTask : DoOnceTask {
  public override TaskStage WhenToCheckIsDone => TaskStage.Before;
  public ExplodeTask(Actor actor) : base(actor) {
  }

  protected override BaseAction GetNextActionImpl() {
    return new WaitBaseAction(actor);
  }
}