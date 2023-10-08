using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Moves slowly.\nSummons a ring of Brambles around you that disappear after 10 turns (needs vision).\nInterrupted when taking damage.", flavorText: "Four limbs and a face are the only bits of humanity left in this husk.")]
public class Thistlebog : AIActor, ITakeAnyDamageHandler {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.MOVE] = 2f,
  };
  protected override ActionCosts actionCosts => StaticActionCosts;
  public override float turnPriority => 50;

  private float cooldown = 0;
  public Thistlebog(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 10;
  }

  internal override (int, int) BaseAttackDamage() => (0, 0);

  public override float Step() {
    var dt = base.Step();
    if (cooldown > 0) {
      cooldown -= dt;
    }
    return dt;
  }

  public void HandleTakeAnyDamage(int damage) {
    if (damage > 0 && task is TelegraphedTask) {
      ClearTasks();
      SetTasks(new WaitTask(this, 1));
      statuses.Add(new SurprisedStatus());
    }
  }

  protected override ActorTask GetNextTask() {
    if (!CanTargetPlayer()) {
      return new MoveRandomlyTask(this, AvoidBrambles);
    }
    var player = GameModel.main.player;
    if (IsNextTo(player)) {
      return new RunAwayTask(this, player.pos, 1, false);
    }
    if (cooldown > 0) {
      return new MoveRandomlyTask(this, AvoidBrambles);
    }
    if (Util.DiamondDistanceToPlayer(this) <= 2) {
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, SummonBramblesAroundPlayer));
    } else {
      return new ChaseTargetTask(this, GameModel.main.player, 1).MaxMoves(1);
    }
  }

  public static bool AvoidBrambles(Tile t) => !(t.grass is Brambles);

  private void SummonBramblesAroundPlayer() {
    if (CanTargetPlayer() && cooldown <= 0) {
      cooldown = 10;
      var center = GameModel.main.player.pos;
      foreach (var tile in floor.GetAdjacentTiles(center)) {
        if (Brambles.CanOccupy(tile) && tile.pos != center) {
          var brambles = new Brambles(tile.pos);
          floor.Put(brambles);
          brambles.AddTimedEvent(10, brambles.KillSelf);
        }
      }
    }
  }
}
