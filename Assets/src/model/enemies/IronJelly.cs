using System;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Invulnerable.\nAttacking the Iron Jelly pushes it away, first attacking any Creature in its way.")]
public class IronJelly : AIActor, IAnyDamageTakenModifier, IBodyTakeAttackDamageHandler {
  public IronJelly(Vector2Int pos) : base(pos) {
    faction = Faction.Neutral;
    hp = baseMaxHp = 99;
    ClearTasks();
  }

  public void HandleTakeAttackDamage(int damage, int hp, Actor source) {
    var isAdjacent = IsNextTo(source);
    var offset = source.pos - pos;
    var newPos = pos - offset;
    if (isAdjacent && offset != Vector2Int.zero && floor.InBounds(newPos)) {
      var pushedTile = floor.tiles[newPos];
      if (pushedTile.body != null) {
        Attack(pushedTile.body);
      }
      if (pushedTile.CanBeOccupied()) {
        pos = pushedTile.pos;
        Perform(new MoveBaseAction(this, pushedTile.pos));
      }
    }
  }

  internal override (int, int) BaseAttackDamage() => (99, 99);

  protected override ActorTask GetNextTask() {
    return new WaitTask(this, 1);
  }

  public int Modify(int input) {
    return 0;
  }
}

[Serializable]
class AttackOrMoveDirectionTask : TelegraphedTask {
  public AttackOrMoveDirectionTask(Actor actor, Vector2Int offset, int turns) :
    base(actor, turns, new AttackOrMoveDirectionBaseAction(actor, offset), ActionType.MOVE) {
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