using System;
using UnityEngine.EventSystems;

public abstract class PlayerInteraction {
  public abstract void Perform();
}

public class SetTasksPlayerInteraction : PlayerInteraction {
  public readonly ActorTask[] tasks;

  public SetTasksPlayerInteraction(params ActorTask[] tasks) {
    this.tasks = tasks;
  }

  public override void Perform() {
    GameModel.main.player.SetTasks(tasks);
  }
}

public class ArbitraryPlayerInteraction : PlayerInteraction {
  public System.Action Action;

  public ArbitraryPlayerInteraction(Action action) {
    Action = action;
  }

  public override void Perform() {
    Action();
  }
}

public interface IPlayerInteractHandler {
  PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData);
}
