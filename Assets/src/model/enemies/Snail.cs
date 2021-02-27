using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Goes into its shell for 3 turns when it takes damage.\nWhile in its shell, it takes 2 less damage.\nPauses after each action.")]
public class Snail : AIActor, IActionPerformedHandler, ITakeAnyDamageHandler {
  public Snail(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 4;
    faction = Faction.Enemy;
    if (MyRandom.value < 0.1f) {
      inventory.AddItem(new ItemSnailShell(1));
    }
  }

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (final.Type != ActionType.WAIT) {
      InsertTasks(new WaitTask(this, 1));
    }
  }

  public void HandleTakeAnyDamage(int dmg) {
    // curl up into your shell
    statuses.Add(new InShellStatus());
  }

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;
    if (isVisible) {
      if (IsNextTo(player)) {
        return new AttackTask(this, player);
      } else {
        return new ChaseTargetTask(this, player);
      }
    } else {
      return new MoveRandomlyTask(this);
    }
  }

  internal override (int, int) BaseAttackDamage() => (2, 2);
}

[Serializable]
[ObjectInfo(spriteName: "snail-shell", flavorText: "A dinky little thing.")]
public class ItemSnailShell : Item, IStackable {
  public ItemSnailShell(int stacks) {
    this.stacks = stacks;
  }
  public int stacksMax => 3;

  private int _stacks;
  public int stacks {
    get => _stacks;
    set {
      if (value < 0) {
        throw new ArgumentException("Setting negative stack!" + this + " to " + value);
      }
      _stacks = value;
      if (_stacks == 0) {
        Destroy();
      }
    }
  }

  public void Throw(Player player, Actor target) {
    target.TakeAttackDamage(3, player);
    stacks--;
  }

  internal override string GetStats() => "Deals 3 damage when thrown.";
}

[System.Serializable]
internal class InShellStatus : StackingStatus, IAttackDamageTakenModifier, IBaseActionModifier {
  public override StackingMode stackingMode => StackingMode.Max;
  public InShellStatus(int stacks) : base(stacks) { }

  public InShellStatus() : this(4) { }

  public override string Info() => "You cannot move or attack but you take 3 less damage!";

  public int Modify(int damage) {
    return damage - 2;
  }

  public BaseAction Modify(BaseAction input) {
    stacks--;
    return new WaitBaseAction(input.actor);
  }
}

[System.Serializable]
internal class SlimedStatus : Status, IActionCostModifier, IBaseActionModifier {
  public override bool isDebuff => true;
  public SlimedStatus() : base() {}

  public ActionCosts Modify(ActionCosts input) {
    input[ActionType.MOVE] *= 2f;
    return input;
  }

  public override string Info() => $"Your next move is 2x slower!";

  public BaseAction Modify(BaseAction input) {
    if (input.Type == ActionType.MOVE) {
      Remove();
    }
    return input;
  }

  public override bool Consume(Status other) => false;
}