using System;
using UnityEngine;

[System.Serializable]
// [ObjectInfo(description: "Doesn't move.\nTelegraphs an attack towards you at range when you're on the same row or column. Jumps next to the location it attacks.")]
[ObjectInfo(description: "Doesn't move.\nAttacks deal no damage but apply poison.")]
public class Snake : AIActor, IDealAttackDamageHandler {
  // attack shortly after the player - this lets the player set up an attack on another target
  // and then get out of the way
  public override float turnPriority => task is AttackGroundTask ? 20 : base.turnPriority;
  public Snake(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 2;
  }

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;
    if (CanTargetPlayer() && IsNextTo(player)) {
      // if (player.pos.x == pos.x || player.pos.y == pos.y) {
      //   var task = new AttackGroundTask(this, player.pos, 1);
      //   var targetPos = player.pos;
      //   task.then = new GenericBaseAction(this, () => AttackLine(targetPos), ActionType.ATTACK);
      //   return task;
      // }
      return new AttackTask(this, player);
    }
    return new WaitTask(this, 1);
  }

  // private void AttackLine(Vector2Int targetPos) {
  //   // find the first obstructor 
  //   var offset = targetPos - pos;
  //   var direction = new Vector2Int(Math.Sign(offset.x), Math.Sign(offset.y));

  //   // guard - max distance 10
  //   for (int d = 0; d < 10; d++) {
  //     var currentTile = floor.tiles[pos + direction];
  //     if (currentTile == null) {
  //       // we're out of bounds
  //       return;
  //     }
  //     if (currentTile.CanBeOccupied()) {
  //       // move there!
  //       Perform(new MoveBaseAction(this, currentTile.pos));
  //       // we might have taken damage or any other number of things by triggering the tiles
  //       if (IsDead) {
  //         return;
  //       }
  //     } else {
  //       // ok, the current tile is occupied. If it's a creature, attack it
  //       Perform(new AttackGroundBaseAction(this, currentTile.pos));
  //       return;
  //     }
  //   }
  // }

  internal override (int, int) BaseAttackDamage() => (0, 0);

  public void HandleDealAttackDamage(int damage, Body target) {
    if (target is Actor a) {
      a.statuses.Add(new PoisonedStatus(1));
    }
  }
}