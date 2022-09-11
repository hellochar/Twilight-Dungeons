using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Every turn, heals a random hurt enemy other than itself for 1 HP.")]
public class Healer : SimpleStatusApplicationEnemy {
  public override int cooldown => 0;
  public Healer(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 5;
  }

  public override void DoTask() {
    var hurtEnemies = floor.Enemies().Where(e => e != this && e.hp < e.maxHp);
    var choice = Util.RandomPick(hurtEnemies);
    if (choice != null) {
      choice.Heal(1);
    }
  }
}

[System.Serializable]
public abstract class SimpleStatusApplicationEnemy : AIActor {
  public virtual int cooldown => 9;
  public virtual bool telegraphs => true;

  public SimpleStatusApplicationEnemy(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    ClearTasks();
    hp = baseMaxHp = 2;
  }

  public virtual bool filter() => true;

  bool justWaited = false;
  protected override ActorTask GetNextTask() {
    if (justWaited) {
      if (CanTargetPlayer() && filter()) {
        if (telegraphs) {
          return new TelegraphedTask(this, 1, new GenericBaseAction(this, TryDoTask));
        } else {
          return new GenericTask(this, TryDoTask);
        }
      } else {
        return new WaitTask(this, 1);
      }
    } else {
      justWaited = true;
      return new WaitTask(this, cooldown);
    }
  }

  public void TryDoTask() {
    if (CanTargetPlayer()) {
      justWaited = false;
      DoTask();
    }
  }

  public abstract void DoTask();

  internal override (int, int) BaseAttackDamage() => (0, 0);
}

[System.Serializable]
[ObjectInfo(description: "Every other turn, applies Poisoned to the Player if visible.")]
public class Poisoner : SimpleStatusApplicationEnemy {
  public Poisoner(Vector2Int pos) : base(pos) {
  }

  public override int cooldown => 1;

  public override void DoTask() {
    GameModel.main.player.statuses.Add(new PoisonedStatus(1));
  }
}

[System.Serializable]
[ObjectInfo(description: "If the Player is next to it, Vulnera applies Vulnerable to the Player, then dies.")]
public class Vulnera : SimpleStatusApplicationEnemy {
  public Vulnera(Vector2Int pos) : base(pos) {
  }

  public override int cooldown => 0;

  public override bool filter() => IsNextTo(GameModel.main.player);

  public override void DoTask() {
    GameModel.main.player.statuses.Add(new VulnerableStatus(20));
    KillSelf();
  }
}

[System.Serializable]
[ObjectInfo(description: "Every other turn, place a Muck next to the Player, if visible.")]
public class Muckola : SimpleStatusApplicationEnemy {
  public Muckola(Vector2Int pos) : base(pos) {
  }

  public override int cooldown => 0;

  public override void DoTask() {
    var player = GameModel.main.player;
    var tile = Util.RandomPick(floor.GetAdjacentTiles(player.pos).Where(t => t is Ground));
    if (tile != null) {
      floor.Put(new Muck(tile.pos));
    }
  }
}

[System.Serializable]
[ObjectInfo(description: "Every other turn, grow a Cheshire Weed Sprout next to the Player if visible.")]
public class Pistrala : SimpleStatusApplicationEnemy {
  public Pistrala(Vector2Int pos) : base(pos) {
  }

  public override int cooldown => 1;
  public override bool telegraphs => true;

  public override void DoTask() {
    var player = GameModel.main.player;
    var pufferPos = 
      Util.RandomPick(floor
        .GetCardinalNeighbors(player.pos)
        .Where(CheshireWeedSprout.CanOccupy)
      );
    if (pufferPos != null) {
      floor.Put(new CheshireWeedSprout(pufferPos.pos));
    }
  }
}
