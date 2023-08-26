using System;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Spawns a Blob upon taking damage.\n\nLeaves a trail of Blob Slime.")]
public class Blobmother : Boss, ITakeAnyDamageHandler, IBodyMoveHandler {
  // moves slightly slower than other blobs so the small blobs get hit first
  public override float turnPriority => task is AttackGroundTask ? 90 : base.turnPriority + 1;
  public Blobmother(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 24;
    faction = Faction.Enemy;
  }

  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);
    // kill all blobs on the map
    var blobs = floor.bodies.Where(b => b is Blob).Cast<Entity>();
    var slime = floor.grasses.Where(b => b is BlobSlime);
    foreach (var b in blobs.Concat(slime).ToArray()) {
      b.Kill(this);
    }
  }

  public void HandleTakeAnyDamage(int damage) {
    if (damage > 0) {
      var blob = new Blob(pos);
      floor.Put(blob);
    }
  }

  internal override (int, int) BaseAttackDamage() {
    return (3, 4);
  }

  protected override ActorTask GetNextTask() {
    if (CanTargetPlayer()) {
      if (IsNextTo(GameModel.main.player)) {
        return new AttackGroundTask(this, GameModel.main.player.pos, 1);
      } else {
        return new ChaseTargetTask(this, GameModel.main.player);
      }
    } else {
      return new MoveRandomlyTask(this);
    }
  }

  public void HandleMove(Vector2Int newPos, Vector2Int oldPos) {
    floor.Put(new BlobSlime(oldPos));
  }
}

[Serializable]
[ObjectInfo("slime", description: "Deals 1 damage to any non-Blob that walks into it.\nRemoved when you walk into it, or the Blobmother dies.")]
public class BlobSlime : Grass, IActorEnterHandler {
  public BlobSlime(Vector2Int pos) : base(pos) {}

  public void HandleActorEnter(Actor who) {
    if (!(who is Blob || who is Blobmother)) {
      who.TakeDamage(1, this);
      Kill(who);
    }
  }
}
