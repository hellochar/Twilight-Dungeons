using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Moves slowly.\nAt range 2, creates a ring of Brambles around you that disappear after 10 turns.", flavorText: "")]
public class Thistlebog : AIActor {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.MOVE] = 2f,
  };
  protected override ActionCosts actionCosts => StaticActionCosts;

  public override float turnPriority => 50;

  public Thistlebog(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 15;
  }

  internal override (int, int) BaseAttackDamage() => (0, 0);

  public override float Step() {
    var dt = base.Step();
    if (cooldown > 0) {
      cooldown -= dt;
    }
    Debug.Log(cooldown);
    return dt;
  }

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;

    if (isVisible && cooldown <= 0) {
      if (Util.DiamondDistanceToPlayer(this) <= 2) {
        return new TelegraphedTask(this, 1, new GenericBaseAction(this, SummonBramblesAroundPlayer));
      } else {
        return new ChaseTargetTask(this, player, 1);
      }
    } else {
      return new MoveRandomlyTask(this);
    }
  }

  private float cooldown = 0;
  private void SummonBramblesAroundPlayer() {
    if (isVisible && cooldown <= 0) {
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
