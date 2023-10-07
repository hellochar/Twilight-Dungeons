using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "If the Hardshell would take more than 2 attack damage, it is reduced to 0.", flavorText: "")]
public class HardShell : AIActor, IAttackDamageTakenModifier {
  public override float turnPriority => 50;
  public HardShell(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 8;
  }

  internal override (int, int) BaseAttackDamage() => (2, 2);

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

  public int Modify(int input) {
    if (input >= 3) {
      input = 0;
    }
    return input;
  }
}
