using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "When attacked, it duplicates into two with half HP.", flavorText: "")]
public class Goo : AIActor, IBodyTakeAttackDamageHandler {
  public override float turnPriority => 50;
  public Goo(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 12;
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

  public void HandleTakeAttackDamage(int damage, int hpBefore, Actor source) {
    if (damage > 0) {
      GameModel.main.EnqueueEvent(Split);
    }
  }

  void Split() {
    if (IsDead) {
      return;
    }
    var hp1 = Mathf.CeilToInt(hp / 2f);
    var hp2 = Mathf.FloorToInt(hp / 2f);

    var goo1 = new Goo(pos);
    goo1.hp = hp1;
    goo1.ClearTasks();
    goo1.statuses.Add(new SurprisedStatus());

    var goo2 = new Goo(pos);
    goo2.hp = hp2;
    goo2.ClearTasks();
    goo2.statuses.Add(new SurprisedStatus());

    floor.PutAll(goo1, goo2);
    // don't die
    floor.Remove(this);
  }
}
