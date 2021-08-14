using System;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Does not move on its own, but telegraphs an attack when you are next to it.\nIf it misses, it will move into the telegraphed position.\nWhen attacked, Iron Jelly gets pushed one tile away, or attacks the creature standing on that Tile.")]
public class IronJelly : AIActor, IBodyTakeAttackDamageHandler {
  public override float turnPriority => task is AttackOrMoveDirectionTask ? 90 : base.turnPriority;
  public IronJelly(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 22;
    ClearTasks();
  }

  public void HandleTakeAttackDamage(int damage, int hp, Actor source) {
    var isAdjacent = IsNextTo(source);
    var offset = source.pos - pos;
    if (isAdjacent && offset != Vector2Int.zero) {
      var pushedTile = floor.tiles[pos - offset];
      if (pushedTile.body == null) {
        Perform(new MoveBaseAction(this, pushedTile.pos));
      } else {
        Perform(new AttackBaseAction(this, pushedTile.body));
      }
    }
    if (task is AttackOrMoveDirectionTask) {
      SetTasks(new WaitTask(this, 1));
      statuses.Add(new SurprisedStatus());
    }
  }

  internal override (int, int) BaseAttackDamage() => (3, 3);

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;
    if (CanTargetPlayer() && IsNextTo(player)) {
      var offset = player.pos - pos;
      return new AttackOrMoveDirectionTask(this, offset, 1);
    }
    return new WaitTask(this, 1);
  }
}

[Serializable]
class AttackOrMoveDirectionTask : TelegraphedTask {
  public AttackOrMoveDirectionTask(Actor actor, Vector2Int offset, int turns) :
    base(actor, 1, new AttackOrMoveDirectionBaseAction(actor, offset), ActionType.WAIT) {
    Offset = offset;
  }

  public Vector2Int Offset { get; }
}

[Serializable]
class AttackOrMoveDirectionBaseAction : BaseAction {
  public override ActionType Type => target == null ? ActionType.MOVE : ActionType.ATTACK;
  public Body target => actor.floor.bodies[actor.pos + offset];
  public readonly Vector2Int offset;

  public AttackOrMoveDirectionBaseAction(Actor actor, Vector2Int offset) : base(actor) {
    this.offset = offset;
  }

  public override void Perform() {
    if (target == null) {
      actor.pos = actor.pos + offset;
    } else {
      actor.AttackGround(actor.pos + offset);
    }
  }
}