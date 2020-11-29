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
  private Actor FindActiveActor() {
    var allActorsInPlay = model.GetAllActorsInPlay();
    return allActorsInPlay.Aggregate((a1, a2) => {
      if (a1.timeNextAction == a2.timeNextAction) {
        return a1.turnPriority < a2.turnPriority ? a1 : a2;
      }
      return a1.timeNextAction < a2.timeNextAction ? a1 : a2;
    });
  }

  internal IEnumerator<object> StepUntilPlayersChoice(Action onEnd) {
    // skip one frame so whatever's currently running can finish (in response to player.action = xxx)
    yield return null;

    model.DrainEventQueue();
    bool isFirstIteration = true;
    do {
      Actor actor = FindActiveActor();
      if (actor == model.player && model.player.action == null) {
        break;
      }

      if (model.time > actor.timeNextAction) {
        throw new Exception("time is " + model.time + " but " + actor + " had a turn at " + actor.timeNextAction);
      }

      if (model.time != actor.timeNextAction) {
        // Debug.Log("Progressing time from " + model.time + " to " + actor.timeNextAction);
        // The first iteration will usually be right after the user's set an action.
        // Do *not* pause in that situation to allow the game to respond instantly.
        if (!isFirstIteration) {
          yield return new WaitForSeconds((actor.timeNextAction - model.time) * 0.2f);
        }
        // move game time up to now
        model.time = actor.timeNextAction;
      }

      actor.Step();

      if (!isFirstIteration && actor.currentTile.visibility == TileVisiblity.Visible) {
        // stagger actors just a bit for juice
        yield return new WaitForSeconds(.02f);
      }

      model.DrainEventQueue();
      isFirstIteration = false;
    } while (true);

    OnPlayersChoice?.Invoke();
    onEnd();
  }
}