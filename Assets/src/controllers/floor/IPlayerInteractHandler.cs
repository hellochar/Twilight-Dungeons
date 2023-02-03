using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

public abstract class PlayerInteraction {
  public abstract void Perform();
}

public class SetTasksPlayerInteraction : PlayerInteraction {
  public readonly List<ActorTask> tasks;

  public SetTasksPlayerInteraction(params ActorTask[] tasks) {
    this.tasks = tasks.ToList();
  }

  public SetTasksPlayerInteraction Then(ActorTask task) {
    tasks.Add(task);
    return this;
  }

  public override void Perform() {
    GameModel.main.player.SetTasks(tasks.ToArray());
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
