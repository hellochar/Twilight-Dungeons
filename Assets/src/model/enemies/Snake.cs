using System;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Only moves or attacks if you're in the same row or column.\n\nAttacks anything in its way.\n\nAttacks apply Weakness.")]
// [ObjectInfo(description: "Chases you.\nAttacks deal no damage but apply poison.")]
public class Snake : AIActor, IDealAttackDamageHandler {
  public override float turnPriority => 20;
  public Snake(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 3;
  }

  // Vector2Int? attackDirection = null;

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;
    // if (attackDirection == null && CanTargetPlayer() && (player.pos.x == pos.x || player.pos.y == pos.y)) {
    //   var offset = player.pos - pos;
    //   attackDirection = new Vector2Int(Math.Sign(offset.x), Math.Sign(offset.y));
    // }
    if (CanTargetPlayer() && (player.pos.x == pos.x || player.pos.y == pos.y)) {
      var offset = player.pos - pos;
      var direction = new Vector2Int(Math.Sign(offset.x), Math.Sign(offset.y));
      var nextPos = pos + direction;
      var target = floor.bodies[nextPos];
      if (floor.tiles[nextPos].CanBeOccupied()) {
        return new MoveToTargetTask(this, nextPos);
      } else if (target != null) {
        return new AttackTask(this, target);
      }
    }

    // if (attackDirection != null) {
    //   Vector2Int direction = attackDirection.Value;
    //   var nextPos = pos + direction;
    //   if (floor.tiles[nextPos].CanBeOccupied()) {
    //     return new MoveToTargetTask(this, nextPos);
    //   } else {
    //     attackDirection = null;
    //     return new AttackGroundTask(this, nextPos, 1);
    //   }
    //   // return new AttackOrMoveDirectionTask(this, direction, 1);
    //   // var task = new AttackGroundTask(this, player.pos, 1);
    //   // var targetPos = player.pos;
    //   // task.then = new GenericBaseAction(this, () => AttackLine(targetPos), ActionType.ATTACK);
    //   // return task;
    // } else {
      return new WaitTask(this, 1);
    // }
    // if (CanTargetPlayer()) {
    //   if (IsNextTo(player)) {
    //     return new AttackTask(this, player);
    //   } else {
    //     return new ChaseTargetTask(this, player);
    //   }
    // } else {
    //   return new WaitTask(this, 1);
    // }
  }

  private void AttackLine(Vector2Int targetPos) {
    // find the first obstructor 
    var offset = targetPos - pos;
    var direction = new Vector2Int(Math.Sign(offset.x), Math.Sign(offset.y));

    // guard - max distance 15
    for (int d = 0; d < 15; d++) {
      var currentTile = floor.tiles[pos + direction];
      if (currentTile == null) {
        // we're out of bounds
        return;
      }
      if (currentTile.CanBeOccupied()) {
        // move there!
        Perform(new MoveBaseAction(this, currentTile.pos));
        // we might have taken damage or any other number of things by triggering the tiles
        if (IsDead) {
          return;
        }
      } else {
        // ok, the current tile is occupied. If it's a creature, attack it
        Perform(new AttackGroundBaseAction(this, currentTile.pos));
        return;
      }
    }
  }

  internal override (int, int) BaseAttackDamage() => (1, 1);

  public void HandleDealAttackDamage(int damage, Body target) {
    if (target is Actor a) {
      a.statuses.Add(new WeaknessStatus(1));
    }
  }
}

[Serializable]
[ObjectInfo("snake-venom", description: "While equipped, your attacks apply poison.")]
public class ItemSnakeVenom : EquippableItem, IAttackHandler, IStackable {
  public ItemSnakeVenom(int stacks) : base() {
    this.stacks = stacks;
  }

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


  public override EquipmentSlot slot => EquipmentSlot.Offhand;

  public void OnAttack(int damage, Body target) {
    if (target is Actor a) {
      a.statuses.Add(new PoisonedStatus(1));
      stacks--;
    }
  }
}