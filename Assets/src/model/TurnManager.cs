using System;
using System.Collections.Generic;
using System.Linq;
using Priority_Queue;
using UnityEngine;

public class TimedEvent {
  public readonly float time;
  public readonly Action action;
  public readonly Entity entity;
  public TimedEvent(Entity e, float time, Action action) {
    this.entity = e;
    this.time = time;
    this.action = action;
    e.OnDeath += HandleDeath;
  }

  private void HandleDeath() {
    GameModel.main.turnManager.RemoveEvent(this);
  }

  public void Done() {
    entity.OnDeath -= HandleDeath;
  }
}

public class TurnManager {
  // private SimplePriorityQueue<Actor, float> queue = new SimplePriorityQueue<Actor, float>();
  private GameModel model { get; }
  public event Action OnPlayersChoice;
  private SimplePriorityQueue<TimedEvent, float> timedEvents = new SimplePriorityQueue<TimedEvent, float>();
  public TurnManager(GameModel model) {
    this.model = model;
  }

  public void AddTimedEvent(TimedEvent evt) {
    timedEvents.Enqueue(evt, evt.time);
  }

  public void RemoveEvent(TimedEvent evt) {
    timedEvents.TryRemove(evt);
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
    int guard = 0;
    do {
      if (guard++ > 1000) {
        Debug.Log("Stopping step because it's been 1000 turns since player had a turn");
        break;
      }

      var entity = FindActiveEntity();

      if (model.time > entity.timeNextAction) {
        throw new Exception("time is " + model.time + " but " + entity + " had a turn at " + entity.timeNextAction);
      }

      if (model.time != entity.timeNextAction) {
        // Debug.Log("Progressing time from " + model.time + " to " + actor.timeNextAction);
        // The first iteration will usually be right after the user's set an action.
        // Do *not* pause in that situation to allow the game to respond instantly.
        if (!isFirstIteration) {
          // speed through long-waiting
          var shouldSpeedThroughWait = (entity == model.player && model.player.task is WaitTask wait && wait.Turns > 3);
          if (!shouldSpeedThroughWait) {
            yield return new WaitForSeconds((entity.timeNextAction - model.time) * 0.2f);
          }
        }
        // move game time up to now
        model.time = entity.timeNextAction;

        // trigger any events that need to happen
        while (timedEvents.Count > 0) {
          var first = timedEvents.First;
          if (first.time > model.time) {
            break;
          }
          first.action();
          first.Done();
          RemoveEvent(first);
        }
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
          // TODO just hack it
          entity.timeNextAction += 1;
          // this actually shouldn't happen to AIs
          Debug.LogWarning(entity + " NoActionException");
        }
      } catch (CannotPerformActionException e) {
        if (entity == model.player) {
          // TODO let the player know
          Debug.LogWarning(e.Message);
          break;
        } else {
          // TODO make this better
          entity.timeNextAction += 1;
        }
      }

      if (model.player.IsDead) {
        // make whole room visible as a hack for now
        model.currentFloor.ForceAddVisibility(model.currentFloor.EnumerateFloor());
        // yield break;
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