using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Attacks at range.\nRuns away if you get too close.")]
public class Octopus : AIActor {
  public override float turnPriority => task is AttackGroundTask ? 90 : base.turnPriority;

  public Octopus(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 3;
  }

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;
    if (IsNextTo(player)) {
      return new RunAwayTask(this, player.pos, 1, true);
    }
    if (CanTargetPlayer()) {
        return new AttackGroundTask(this, GameModel.main.player.pos, 1);
    } else {
      return new WaitTask(this, 1);
    }
  }

  internal override (int, int) BaseAttackDamage() => (1, 2);
}
