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

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;
    if (CanTargetPlayer()) {
      if (IsNextTo(player)) {
        return new AttackTask(this, player);
      } else {
        var jumpTile = floor.EnumerateCircle(pos, 3f)
          .Where(pos => Util.DiamondMagnitude(pos - this.pos) == 2)
          .Select(pos => floor.tiles[pos])
          .Where(t => t.CanBeOccupied())
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
