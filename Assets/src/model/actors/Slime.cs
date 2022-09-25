using System;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Blocks your path. Leaves a debuff when you kill it")]
public class Slime : Destructible, IDeathHandler {
  public Slime(Vector2Int pos) : base(pos, 3) {
  }

  public void HandleDeath(Entity source) {
    GameModel.main.player.water += 60;
    GameModel.main.player.statuses.Add(new SlimyStatus(1, GameModel.main.currentFloor.depth));
    floor.Put(new WallTrigger(pos));
  }
}

[Serializable]
class WallTrigger : Trigger, IActorLeaveHandler {
  public WallTrigger(Vector2Int pos) : base(pos) {}

  public void HandleActorLeave(Actor who) {
    if (who is Player) {
      KillSelf();
      who.floor.Put(new Wall(pos));
#if experimental_retryondemand
      GameModel.main.EnqueueEvent(() => {
        GameModel.main.DrainEventQueue();
        Serializer.SaveMainToLevelStart();
      });
#endif
    }
  }
}

[System.Serializable]
[ObjectInfo("slimed", "You're filled with sticky slime! You take more damage.")]
internal class SlimyStatus : StackingStatus, IAttackDamageTakenModifier {
  public override bool isDebuff => true;
  public override StackingMode stackingMode => StackingMode.Add;
  public int depth;
  public SlimyStatus(int stacks, int depth) : base(stacks) {
    this.depth = depth;
  }

  public override string Info() => $"You take {stacks} more attack damage.";

  public override void Step() {
    if (GameModel.main.currentFloor.depth != depth) {
      Remove();
    }
  }

  public int Modify(int input) {
    return input + stacks;
  }
}
