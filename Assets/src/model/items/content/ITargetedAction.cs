using System.Collections.Generic;

public interface ITargetedAction<out T> where T : Entity {
  string TargettedActionName { get; }
  IEnumerable<T> Targets(Player player);
  void PerformTargettedAction(Player player, Entity target);
}

public static class ITargetedActionExtensions {
  public static async void ShowTargetingUIThenPerform<T>(this ITargetedAction<T> action, Player player) where T : Entity {
    var floor = player.floor;
    try {
      var target = await MapSelector.SelectUI(action.Targets(player));
      action.PerformTargettedAction(player, target);
      GameModel.main.DrainEventQueue();
    } catch (PlayerSelectCanceledException) {
    } catch (CannotPerformActionException e) {
      GameModel.main.turnManager.OnPlayerCannotPerform(e);
    }
  }
}