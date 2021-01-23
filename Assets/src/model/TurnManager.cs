using System;
using System.Collections;
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
  public event Action<ISteppable> OnStep;
  public event Action OnTimePassed;
  public event Action<CannotPerformActionException> OnPlayerCannotPerform;
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
  private ISteppable FindActiveEntity() {
    var allActorsInPlay = model.GetAllEntitiesInPlay();
    return allActorsInPlay.Aggregate((a1, a2) => {
      if (a1.timeNextAction == a2.timeNextAction) {
        return a1.turnPriority < a2.turnPriority ? a1 : a2;
      }
      return a1.timeNextAction < a2.timeNextAction ? a1 : a2;
    });
  }

  internal IEnumerator StepUntilPlayersChoice() {
    var enumerator = StepUntilPlayersChoiceImpl();
    while (true) {
      var hasNext = false;
      #if UNITY_EDITOR
      hasNext = enumerator.MoveNext();
      #else
      try {
        hasNext = enumerator.MoveNext();
      } catch (Exception e) {
        Debug.LogError(e);
        Messages.Create(e.Message);
      }
      #endif

      if (hasNext) {
        yield return enumerator.Current;
      } else {
        break;
      }
    }

    OnPlayersChoice?.Invoke();
  }

  public static float JUICE_STAGGER_SECONDS = 0.02f;
  public static float GAME_TIME_TO_SECONDS_WAIT_SCALE = 0.2f;
  private IEnumerator StepUntilPlayersChoiceImpl() {
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
          var shouldSpeedThroughLongWalk = (entity == model.player && model.player.task is FollowPathTask path && path.path.Count > 10);
          if (!shouldSpeedThroughWait && !shouldSpeedThroughLongWalk) {
            yield return new WaitForSeconds((entity.timeUntilTurn()) * GAME_TIME_TO_SECONDS_WAIT_SCALE);
          }
        }
        // move game time up to now
        model.time = entity.timeNextAction;
        OnTimePassed?.Invoke();

        // trigger any events that need to happen
        while (timedEvents.Count > 0) {
          var first = timedEvents.First;
          if (first.time > model.time) {
            break;
          }
          first.action();
          first.Done();
          RemoveEvent(first);
          model.DrainEventQueue();
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
          OnPlayerCannotPerform?.Invoke(e);
          break;
        } else {
          // TODO make this better
          entity.timeNextAction += 1;
        }
      } catch (ActorDiedException) {
        // just a catch-all; we don't have to do anything
      }

      if (model.player.IsDead) {
        // make whole room visible as a hack for now
        model.currentFloor.ForceAddVisibility(model.currentFloor.EnumerateFloor());
        // yield break;
      }

      if (!isFirstIteration && entity is Actor a && a.isVisible) {
        // stagger actors just a bit for juice
        yield return new WaitForSeconds(JUICE_STAGGER_SECONDS);
      }

      model.DrainEventQueue();
      OnStep?.Invoke(entity);
      isFirstIteration = false;
    } while (true);
  }
}