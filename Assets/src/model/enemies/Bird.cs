using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Jumps two tiles per turn and waits after every jump.")]
public class Bird : AIActor, IActionPerformedHandler {
  // public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
  //   [ActionType.MOVE] = 2,
  // };

  // protected override ActionCosts actionCosts => Bird.StaticActionCosts;
  public Bird(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 3;
  }

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (final.Type == ActionType.MOVE) {
      InsertTasks(new WaitTask(this, 1));
    }
  }

  public static IEnumerable<Tile> GetJumpTiles(Entity e) {
    return e.floor.EnumerateCircle(e.pos, 3f)
      .Where(pos => Util.DiamondMagnitude(pos - e.pos) == 2)
      .Select(pos => e.floor.tiles[pos])
      .Where(t => t.CanBeOccupied());
  }

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;
    if (CanTargetPlayer()) {
      if (IsNextTo(player)) {
        return new AttackTask(this, player);
      } else {
        var jumpTile = GetJumpTiles(this)
          .OrderBy(t => t.DistanceTo(player))
          .FirstOrDefault();
        if (jumpTile != null) {
          return new JumpToTargetTask(this, jumpTile.pos);
        } else {
          return new WaitTask(this, 1);
        }
      }
    } else {
      return new MoveRandomlyTask(this);
    }
  }

  internal override (int, int) BaseAttackDamage() => (1, 2);
}

[Serializable]
[ObjectInfo("bird-wings")]
public class ItemBirdWings : EquippableItem, ITargetedAction<Tile>, IStackable {
  public override EquipmentSlot slot => EquipmentSlot.Armor;

  string ITargetedAction<Tile>.TargettedActionName => "Fly";

  string ITargetedAction<Tile>.TargettedActionDescription => "Choose where to land.";

  public int stacksMax => 100;

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

  public ItemBirdWings(int stacks) : base() {
    this.stacks = stacks;
  }

  void ITargetedAction<Tile>.PerformTargettedAction(Player player, Entity target) {
    player.SetTasks(new JumpToTargetTask(player, target.pos));
    stacks--;
  }

  IEnumerable<Tile> ITargetedAction<Tile>.Targets(Player player) {
    return Bird.GetJumpTiles(player).OfType<Tile>();
  }
}