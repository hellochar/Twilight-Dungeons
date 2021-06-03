using System;
using System.Linq;
using UnityEngine;

[Serializable]
public abstract class Boss : AIActor {
  protected Boss(Vector2Int pos) : base(pos) { }
}

[Serializable]
[ObjectInfo(description: "Spawns a Blob upon taking damage.\nLeaves a trail of Blob Slime.")]
public class Blobmother : Boss, ITakeAnyDamageHandler, IBodyMoveHandler {
  public override float turnPriority => task is AttackGroundTask ? 90 : base.turnPriority;
  public Blobmother(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 24;
    faction = Faction.Enemy;
  }

  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);
    // kill all blobs on the map
    var blobs = floor.bodies.Where(b => b is Blob).Cast<Blob>().ToList();
    foreach (var b in blobs) {
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
    floor.Put(new BlobSlime(oldPos));
  }
}

[Serializable]
[ObjectInfo("slime", description: "Deals 1 damage to any non-Blob that walks into it.\nLasts 12 turns.")]
public class BlobSlime : Grass, IActorEnterHandler {
  public BlobSlime(Vector2Int pos) : base(pos) {}

  protected override void HandleEnterFloor() {
    AddTimedEvent(12, KillSelf);
  }

  public void HandleActorEnter(Actor who) {
    if (!(who is Blob || who is Blobmother)) {
      who.TakeDamage(1, this);
      Kill(who);
    }
  }
}

// public class SporeColonyBoss : AIActor {
//   public SporeColonyBoss(Vector2Int pos) : base(pos) {
//     hp = baseMaxHp = 
//     faction = Faction.Enemy;
//   }

//   protected override ActorTask GetNextTask() {
//     throw new NotImplementedException();
//   }
// }
