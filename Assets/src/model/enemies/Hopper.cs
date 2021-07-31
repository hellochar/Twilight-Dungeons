using System;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Jumps next to you.\nWhen hurt, it will eat a nearby Grass to heal itself to full HP.")]
public class Hopper : AIActor {
  public Hopper(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 6;
  }

  internal override (int, int) BaseAttackDamage() => (2, 2);

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;

    if (hp != maxHp) {
      if (grass != null) {
        return new TelegraphedTask(this, 1, new GenericBaseAction(this, EatGrass));
      } else {
        var nearbyGrassTile = floor
          .BreadthFirstSearch(pos, t => t.CanBeOccupied() && DistanceTo(t) < 5)
          .Where(t => t.grass != null)
          .FirstOrDefault();
        if (nearbyGrassTile != null) {
          return new MoveToTargetTask(this, nearbyGrassTile.pos);
        } else {
          return new MoveRandomlyTask(this);
        }
      }
    } else {
      if (CanTargetPlayer()) {
        if (IsNextTo(player)) {
          return new AttackTask(this, player);
        } else {
          var jumpTile = floor.GetAdjacentTiles(player.pos).Where(t => t.CanBeOccupied()).OrderBy(t => t.DistanceTo(pos)).FirstOrDefault();
          return new JumpToTargetTask(this, jumpTile.pos);
        }
      } else {
        return new MoveRandomlyTask(this);
      }
    }
  }

  void EatGrass() {
    if (grass != null) {
      Heal(maxHp - hp);
      grass.Kill(this);
    }
  }
}

[Serializable]
class JumpToTargetTask : DoOnceTask {
  public JumpToTargetTask(Actor actor, Vector2Int position) : base(actor) {
    this.position = position;
  }

  public Vector2Int position { get; }

  protected override BaseAction GetNextActionImpl() {
    return new JumpBaseAction(actor, position);
  }
}

public class JumpBaseAction : BaseAction {
  public override ActionType Type => ActionType.MOVE;
  public readonly Vector2Int pos;

  public JumpBaseAction(Actor actor, Vector2Int pos) : base(actor) {
    this.pos = pos;
  }

  public override void Perform() {
    actor.pos = pos;
  }
}