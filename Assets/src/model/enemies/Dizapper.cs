using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Applies Vulnerable when it hits you.\nGets stunned when attacked.", flavorText: "")]
public class Dizapper : AIActor, IAttackHandler, IBodyTakeAttackDamageHandler {
  public override string displayName => "Ghost";
  public override float turnPriority => 60;
  public Dizapper(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 9;
  }

  internal override (int, int) BaseAttackDamage() => (1, 1);

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;
    if (CanTargetPlayer()) {
      if (IsNextTo(player)) {
        return new AttackTask(this, player);
      } else {
        return new ChaseTargetTask(this, player);
      }
    } else {
      return new MoveRandomlyTask(this);
    }
  }

  public void OnAttack(int damage, Body target) {
    if (target is Actor a) {
      a.statuses.Add(new VulnerableStatus(10));
    }
  }

  public void HandleTakeAttackDamage(int damage, int hp, Actor source) {
    if (damage > 0) {
      SetTasks(new WaitTask(this, 1));
      statuses.Add(new SurprisedStatus());
    }
  }
}
