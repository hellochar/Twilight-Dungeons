using System;
using System.Collections.Generic;
using UnityEngine;

public class Boombug : AIActor {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.WAIT] = 2f,
    [ActionType.MOVE] = 2f,
  };
  protected override ActionCosts actionCosts => Boombug.StaticActionCosts;
  public Boombug(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 1;
    faction = Faction.Neutral;
    ai = AIs.BugAI(this).GetEnumerator();
    OnDeath += HandleDeath;
  }

  private void HandleDeath() {
    var floor = this.floor;
    // leave a corpse on death
    // explode and hurt everything nearby
    GameModel.main.EnqueueEvent(() => floor.Put(new BoombugCorpse(pos)));
  }
}

[ObjectInfo(spriteName: "boombug", flavorText: "Evolution sure comes up with crazy shit sometimes...")]
public class ItemBoombugCorpse : Item, IStackable {
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

  public void Throw(Player player, Vector2Int position) {
    player.floor.Put(new BoombugCorpse(position));
    stacks--;
  }
}

public class BoombugCorpse : Actor {
  private bool exploded = false;
  public event Action OnExploded;
  public BoombugCorpse(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 1;
    timeNextAction += 1;
    OnDeath += HandleDeath;
    SetTasks(new ExplodeTask(this));
  }

  private void HandleDeath() {
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
    exploded = true;
    foreach (var tile in floor.GetAdjacentTiles(pos)) {
      OnExploded?.Invoke();
      this.AttackGround(tile.pos);
    }
    Kill();
  }

  internal override (int, int) BaseAttackDamage() {
    return (3, 3);
  }
}

public class ExplodeTask : ActorTask {
  public ExplodeTask(Actor actor) : base(actor) {
  }

  public override IEnumerator<BaseAction> Enumerator() {
    yield return new WaitBaseAction(actor);
  }
}