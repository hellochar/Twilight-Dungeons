using System;
using System.Collections.Generic;
using UnityEngine;

[ObjectInfo(description: "Goes into its shell when it takes damage.\nPauses after each action.\nChases you.")]
public class Snail : AIActor {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.WAIT] = 1f,
    [ActionType.ATTACK] = 1f,
    [ActionType.MOVE] = 1f,
  };

  protected override ActionCosts actionCosts => Snail.StaticActionCosts;
  public Snail(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 4;
    faction = Faction.Enemy;
    ai = AI().GetEnumerator();
    OnTakeAnyDamage += HandleTakeDamage;
    if (UnityEngine.Random.value < 0.1f) {
      inventory.AddItem(new ItemSnailShell(1));
    }
    OnActionPerformed += HandleActionPerformed;
    // OnMove += HandleMove;
  }

  private void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (final.Type != ActionType.WAIT) {
      InsertTasks(new WaitTask(this, 1));
    }
  }

  // private void HandleMove(Vector2Int newPos, Vector2Int oldPos) {
  //   floor.Put(new GrassSlimeTrail(oldPos));
  // }

  private void HandleTakeDamage(int dmg) {
    // curl up into your shell
    statuses.Add(new InShellStatus());
  }

  private IEnumerable<ActorTask> AI() {
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

  internal override (int, int) BaseAttackDamage() => (2, 2);
}

[ObjectInfo(spriteName: "snail-shell", flavorText: "A dinky little thing.")]
public class ItemSnailShell : Item, IStackable {
  public ItemSnailShell(int stacks) {
    this.stacks = stacks;
  }
  public int stacksMax => 1;

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

// internal class GrassSlimeTrail : Grass {
//   private Vector2Int oldPos;
//   int timeAlive = 5;

//   public GrassSlimeTrail(Vector2Int pos) : base(pos) {
//     timeNextAction = timeCreated + 1;
//     OnEnterFloor += HandleEnterFloor;
//     OnLeaveFloor += HandleLeaveFloor;
//   }

//   private void HandleEnterFloor() {

//   }

//   private void HandleLeaveFloor() {
//     throw new NotImplementedException();
//   }

//   protected override float Step() {
//     if (age > timeAlive) {
//       Kill();
//     }
//     return 1;
//   }
// }

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