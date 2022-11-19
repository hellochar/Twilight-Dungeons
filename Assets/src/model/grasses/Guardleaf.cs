using System;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Blocks up to 5 attack damage dealt to the creature standing on the Guardleaf.", flavorText: "Huge leaves, sprouting out from the ground, gently twist themselves around you in a protective cover.")]
public class Guardleaf : Grass, IActorEnterHandler {
  public static Item HomeItem => new ItemGuardleafCutting();
  public static bool CanOccupy(Tile tile) => tile is Ground;
  public int guardLeft;
  public Guardleaf(Vector2Int pos) : base(pos) {
    guardLeft = 5;
  }

  protected override void HandleEnterFloor() {
    actor?.statuses.Add(new GuardedStatus());
  }

  protected override void HandleLeaveFloor() {
    actor?.statuses.RemoveOfType<GuardedStatus>();
  }

  public void HandleActorEnter(Actor who) {
    who.statuses.Add(new GuardedStatus());
    OnNoteworthyAction();
  }

  internal void removeGuard(int reduction) {
    guardLeft -= reduction;
    if (guardLeft <= 0) {
      GameModel.main.EnqueueEvent(KillSelf);
    }
  }
}

[Serializable]
[ObjectInfo("guardleaf", description: "Use to grow a Guardleaf at your position.")]
public class ItemGuardleafCutting : Item, IUsable {
  public void Use(Actor a) {
    a.floor.Put(new Guardleaf(a.pos));
    stacks--;
  }
}

[System.Serializable]
[ObjectInfo("guardroot", "Huge leaves, sprouting out from the ground, gently twist themselves around you in a protective cover.")]
public class GuardedStatus : StackingStatus, IAttackDamageTakenModifier {
  public override int stacks {
    get => leaf?.guardLeft ?? 0;
    set { }
  }

  public GuardedStatus() {}

  private Guardleaf leaf => actor?.grass as Guardleaf;
  public override string Info() => $"The Guardleaf will block {stacks} more attack damage.";

  public int Modify(int input) {
    if (leaf != null) {
      var reduction = Mathf.Min(input, leaf.guardLeft);
      leaf.removeGuard(reduction);
      leaf.OnNoteworthyAction();
      return input - reduction;
    } else {
      Remove();
      return input;
    }
  }

  public override void Step() {
    if (leaf == null || stacks <= 0) {
      Remove();
    }
  }

  public override bool Consume(Status other) => true;
}
