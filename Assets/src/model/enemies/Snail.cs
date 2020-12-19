using System;
using System.Collections.Generic;
using UnityEngine;

public class Snail : AIActor {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.WAIT] = 1.5f,
    [ActionType.ATTACK] = 1.5f,
    [ActionType.MOVE] = 1.5f,
  };

  protected override ActionCosts actionCosts => Snail.StaticActionCosts;
  public Snail(Vector2Int pos) : base(pos) {
    hp = hpMax = 9;
    faction = Faction.Enemy;
    ai = AI().GetEnumerator();
    OnAttack += HandleAttack;
    OnTakeDamage += HandleTakeDamage;
  }

  private void HandleTakeDamage(int dmg, int hp, Actor source) {
    // curl up into your shell
    statuses.Add(new InShellStatus());
  }

  private void HandleAttack(int dmg, Actor who) {
    // who.statuses.Add(new SlimedStatus());
  }

  private IEnumerable<ActorTask> AI() {
    yield return new SleepTask(this);
    while (true) {
      if (isVisible) {
        if (IsNextTo(GameModel.main.player)) {
          yield return new AttackTask(this, GameModel.main.player);
        } else {
          while (!actor.IsNextTo(GameModel.main.player)) {
            yield return new ChaseTargetTask(this, GameModel.main.player);
          }
        }
      } else {
        yield return new MoveRandomlyTask(this);
      }
    }
  }

  internal override int BaseAttackDamage() {
    return UnityEngine.Random.Range(2, 4);
  }
}

internal class InShellStatus : StackingStatus, IDamageTakenModifier, IBaseActionModifier {
  public override StackingMode stackingMode => StackingMode.Max;
  public InShellStatus(int stacks) : base(stacks) { }

  public InShellStatus() : this(3) { }

  public override string Info() => "You cannot move or attack but you take 3 less damage!";

  public int Modify(int damage) {
    return damage - 3;
  }

  public BaseAction Modify(BaseAction input) {
    stacks--;
    return new WaitBaseAction(input.actor);
  }
}

internal class SlimedStatus : Status, IActionCostModifier, IBaseActionModifier {
  public SlimedStatus() : base() {}

  public ActionCosts Modify(ActionCosts input) {
    input[ActionType.ATTACK] *= 2f;
    return input;
  }

  public override string Info() => $"You attack 2x slower!\nBroken by movement.";

  public BaseAction Modify(BaseAction input) {
    if (input.Type == ActionType.MOVE) {
      Remove();
    }
    return input;
  }

  public override void Stack(Status other) { }
}