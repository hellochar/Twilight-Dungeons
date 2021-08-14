using System;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "When a creature walks over a Dandypuff, they gain Weakness, dealing -1 damage on their next attack.")]
public class Dandypuff : Grass, IActorEnterHandler {
  public static bool CanOccupy(Tile t) => t is Ground;
  public Dandypuff(Vector2Int pos) : base(pos) {}

  public void HandleActorEnter(Actor who) {
    if ((who is Player)) {
      who.statuses.Add(new DandyStatus(1));
      Kill(who);
    }
  }
}

[Serializable]
[ObjectInfo("dandypuff")]
public class DandyStatus : StackingStatus, IBodyTakeAttackDamageHandler, IAttackHandler {
  public DandyStatus(int stacks) : base(stacks) {}
  public override string Info() => $"Collect five to make.\nRemoved when you take damage.";

  public void HandleTakeAttackDamage(int damage, int hp, Actor source) {
    if (damage > 0) {
      stacks = 0;
    }
  }

  public void OnAttack(int damage, Body target) {
    // if (target is Actor a) {
    //   // stacks--;
    //   // a.ClearTasks();
    //   a.SetTasks(new WaitTask(a, 1));
    //   a.statuses.Add(new SurprisedStatus());
    //   // actor.SwapPositions(a);
    // }
  }
}

[Serializable]
[ObjectInfo(description: "Leaves a trail of Dandypuffs.\nWhen a creature walks over a Dandypuff, they gain Weakness, dealing -1 damage on their next attack.")]
public class Dandyslug : AIActor, IBodyMoveHandler {
  // public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
  //   [ActionType.WAIT] = 2f,
  //   [ActionType.MOVE] = 2f,
  // };
  // protected override ActionCosts actionCosts => Dandyslug.StaticActionCosts;
  public Dandyslug(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 3;
    faction = Faction.Neutral;
    ClearTasks();
  }

  protected override ActorTask GetNextTask() {
    var range5Tiles = floor.EnumerateCircle(pos, 5).Where((pos) => floor.tiles[pos].CanBeOccupied());
    var target = Util.RandomPick(range5Tiles);
    return new MoveToTargetTask(this, target);
  }

  internal override (int, int) BaseAttackDamage() => (2, 2);

  public void HandleMove(Vector2Int newPos, Vector2Int oldPos) {
    floor.Put(new Dandypuff(oldPos));
  }
}