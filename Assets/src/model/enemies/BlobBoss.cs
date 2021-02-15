using System;
using System.Linq;
using UnityEngine;

[Serializable]
// a boss!
[ObjectInfo(description: "Spawns a Blob upon taking any damage.\nDestroys any grass it steps over.")]
public class BlobBoss : AIActor, ITakeAnyDamageHandler, IBodyMoveHandler {
  public override float turnPriority => task is AttackGroundTask ? 90 : base.turnPriority;
  public BlobBoss(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 32;
    faction = Faction.Enemy;
  }

  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);
  }

  public void HandleTakeAnyDamage(int damage) {
    if (damage > 0) {
      var tile = floor.BreadthFirstSearch(pos, tile => tile.CanBeOccupied()).Skip(1).FirstOrDefault();
      if (tile != null) {
        var blob = new Blob(tile.pos);
        floor.Put(blob);
      }
    }
  }

  internal override (int, int) BaseAttackDamage() {
    return (3, 4);
  }

  protected override ActorTask GetNextTask() {
    if (isVisible) {
      if (IsNextTo(GameModel.main.player)) {
        // TODO make blob attack a 3x3 area, telegraphs for 2 turns
        return new AttackGroundTask(this, GameModel.main.player.pos, 1);
      } else {
        return new ChaseTargetTask(this, GameModel.main.player);
      }
    } else {
      return new MoveRandomlyTask(this);
    }
  }

  public void HandleMove(Vector2Int newPos, Vector2Int oldPos) {
    floor.grasses[newPos]?.Kill(this);
    floor.grasses[oldPos]?.Kill(this);
  }
}
