using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager {
  // private SimplePriorityQueue<Actor, float> queue = new SimplePriorityQueue<Actor, float>();
  private GameModel model { get; }
  public event Action OnPlayersChoice;
  public TurnManager(GameModel model) {
    this.model = model;
  }

  /// The actor whose turn it is
  private SteppableEntity FindActiveEntity() {
    var allActorsInPlay = model.GetAllEntitiesInPlay();
    return allActorsInPlay.Aggregate((a1, a2) => {
      if (a1.timeNextAction == a2.timeNextAction) {
        return a1.turnPriority < a2.turnPriority ? a1 : a2;
      }
      return a1.timeNextAction < a2.timeNextAction ? a1 : a2;
    });
  }

  internal IEnumerator<object> StepUntilPlayersChoice() {
    // skip one frame so whatever's currently running can finish (in response to player.action = xxx)
    yield return null;

    model.DrainEventQueue();
    bool isFirstIteration = true;
    do {
      var entity = FindActiveEntity();

      if (model.time > entity.timeNextAction) {
        throw new Exception("time is " + model.time + " but " + entity + " had a turn at " + entity.timeNextAction);
      }

      if (model.time != entity.timeNextAction) {
        // Debug.Log("Progressing time from " + model.time + " to " + actor.timeNextAction);
        // The first iteration will usually be right after the user's set an action.
        // Do *not* pause in that situation to allow the game to respond instantly.
        if (!isFirstIteration && entity.isVisible) {
          yield return new WaitForSeconds((entity.timeNextAction - model.time) * 0.2f);
        }
        // move game time up to now
        model.time = entity.timeNextAction;
      }

      if (entity == model.player && model.player.task == null) {
        break;
      }

      try {
        entity.DoStep();
      } catch (NoActionException) {
        if (entity == model.player) {
          // stop if it's the player
          break;
        } else {
          // just hack it
          entity.timeNextAction += 1;
          // this actually shouldn't happen to AIs
          Debug.LogWarning(entity + " NoActionException");
        }
      }

      if (!isFirstIteration && entity.isVisible) {
        // stagger actors just a bit for juice
        yield return new WaitForSeconds(.02f);
      }

      model.DrainEventQueue();
      isFirstIteration = false;
    } while (true);

    OnPlayersChoice?.Invoke();
  }
}