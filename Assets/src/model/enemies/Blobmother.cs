using System;
using System.Linq;
using UnityEngine;

[Serializable]
public abstract class Boss : AIActor {
  public bool isSeen = false;

  internal bool EnsureSeen() {
    if (!isSeen) {
      isSeen = true;
      return true;
    }
    return false;
  }

  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);
    floor.Put(new HeartTrigger(pos));
  }

  protected Boss(Vector2Int pos) : base(pos) { }
}

[Serializable]
class HeartTrigger : Trigger {
  public HeartTrigger(Vector2Int pos) : base(pos, null) {
  }

  public override void HandleActorEnter(Actor who) {
    if (who is Player player) {
      player.AddMaxHP(4);
      player.Replenish();
      SpriteFlyAnimation.Create(MasterSpriteAtlas.atlas.GetSprite("heart_animated_2_0"), Util.withZ(pos), GameObject.Find("Hearts"));
      KillSelf();
    }
  }
}

[Serializable]
[ObjectInfo(description: "Spawns a Blob upon taking damage.\nLeaves a trail of Blob Slime.\nRemoves Blobs and Blob Slime on death.")]
public class Blobmother : Boss, ITakeAnyDamageHandler, IBodyMoveHandler {
  // moves slightly slower than other blobs so the small blobs get hit first
  public override float turnPriority => task is AttackGroundTask ? 90 : base.turnPriority + 1;
  public Blobmother(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 36;
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
    if (isVisible) {
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

// public class SporeColonyBoss : AIActor {
//   public SporeColonyBoss(Vector2Int pos) : base(pos) {
//     hp = baseMaxHp = 
//     faction = Faction.Enemy;
//   }

//   protected override ActorTask GetNextTask() {
//     throw new NotImplementedException();
//   }
// }
