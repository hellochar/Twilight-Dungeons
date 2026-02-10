using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface ITargetedAction<out T> where T : Entity {
  string TargettedActionName { get; }
  string TargettedActionDescription { get; }
  IEnumerable<T> Targets(Player player);
  void PerformTargettedAction(Player player, Entity target);
}

public static class ITargetedActionExtensions {
  public static async void ShowTargetingUIThenPerform(this ITargetedAction<Entity> action, Player player) {
    var floor = player.floor;
    try {
      var targets = action.Targets(player);
      if (targets == null || !targets.Any()) {
        throw new CannotPerformActionException("No valid targets!");
      }
      Sprite previewSprite = null;
      if (action is Item item) {
        previewSprite = ObjectInfo.GetSpriteFor(item);
      }
      var target = await MapSelector.SelectUI(targets.ToList(), action.TargettedActionDescription, previewSprite);
      action.PerformTargettedAction(player, target);
      GameModel.main.DrainEventQueue();
    } catch (PlayerSelectCanceledException) {
    } catch (CannotPerformActionException e) {
      GameModel.main.turnManager.OnPlayerCannotPerform(e);
    }
  }
}