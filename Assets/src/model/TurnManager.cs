using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Priority_Queue;
using UnityEngine;

[Serializable]
public class TurnManager {
  // private SimplePriorityQueue<Actor, float> queue = new SimplePriorityQueue<Actor, float>();
  private GameModel model { get; }
  [field:NonSerialized] /// controller only
  public event Action OnPlayersChoice;
  [field:NonSerialized] /// controller only
  public event Action<ISteppable> OnStep;
  [field:NonSerialized] /// controller only
  public event Action OnTimePassed;
  [field:NonSerialized] /// controller only
  public Action<CannotPerformActionException> OnPlayerCannotPerform = delegate {};
  public ISteppable activeEntity;
  public bool forceStaggerThisTurn = false;
  [NonSerialized]
  public Exception latestException;

  public float timeNextDay;
  public const float TURNS_PER_DAY = 100f;
  public TurnManager(GameModel model) {
    this.model = model;
    timeNextDay = model.time + TURNS_PER_DAY;
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
    try {
      while (true) {
        if (GameModel.main.player.IsDead) {
          break;
        }
        var hasNext = false;
        #if UNITY_EDITOR
        hasNext = enumerator.MoveNext();
        #else
        try {
          hasNext = enumerator.MoveNext();
        } catch (Exception e) {
          Debug.LogError(e);
          latestException = e;
        }
        #endif

        if (hasNext) {
          yield return enumerator.Current;
        } else {
          break;
        }
      }
    } finally {
      OnPlayersChoice?.Invoke();
    }
  }

  public static float JUICE_STAGGER_SECONDS = 0.02f;
  public static float GAME_TIME_TO_SECONDS_WAIT_SCALE = 0.2f;
  private IEnumerator StepUntilPlayersChoiceImpl() {
    // skip one frame so whatever's currently running can finish (in response to player.action = xxx)
    yield return null;

    model.DrainEventQueue();
    bool isFirstIteration = true;
    int guard = 0;
    bool playerTookATurn = false;
    bool enemyTookATurn = false;
    do {
      if (guard++ > 1000 && !model.player.IsDead) {
        Debug.Log("Stopping step because it's been 1000 turns since player had a turn");
        break;
      }

      var entity = FindActiveEntity();
      activeEntity = entity;

      if (model.time > entity.timeNextAction) {
        Debug.LogError("time is " + model.time + " but " + entity + " had a turn at " + entity.timeNextAction);
        // force it to be now. hacky but prevents game deadlocks
        entity.timeNextAction = model.time;
        // throw new Exception();
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

        if (timeNextDay <= entity.timeNextAction) {
          // step the day
          model.time = timeNextDay;
          yield return GameModelController.main.StepDayCoroutine();
          timeNextDay = model.time + TURNS_PER_DAY;
        }
        
        // move game time up to now
        model.time = entity.timeNextAction;
        OnTimePassed?.Invoke();

        // trigger any events that need to happen
        for(var evt = model.timedEvents.Next(); evt != null && evt.time <= model.time; evt = model.timedEvents.Next()) {
          /// if the timed event is registered for an inactive floor
          /// (aka a depth 3 entity, when we're on depth 4) - just remove it
          if (evt.owner.floor != model.currentFloor) {
            model.timedEvents.Unregister(evt);
            continue;
          }
          evt.action();
          model.timedEvents.Unregister(evt);
          model.DrainEventQueue();
        }
      }

      if (entity == model.player) {
        var noMoreTasks = model.player.task == null;
        var worldHasChanged = playerTookATurn && enemyTookATurn;
        if (noMoreTasks || worldHasChanged) {
          break;
        }
      } else if (entity is AIActor a && a.faction == Faction.Enemy) {
        enemyTookATurn = true;
      }

      try {
        entity.DoStep();
      } catch (NoActionException) {
        if (entity == model.player) {
          // stop turn loop if it's the player
          break;
        } else {
          // TODO just hack it
          entity.timeNextAction += 1;
          // this actually shouldn't happen to AIs
          Debug.LogWarning(entity + " NoActionException");
        }
      } catch (CannotPerformActionException exception) {
        if (entity == model.player) {
          OnPlayerCannotPerform(exception);
          break;
        } else {
          // TODO make this better
          entity.timeNextAction += 1;
        }
      } catch (ActorDiedException) {
        // just a catch-all; we don't have to do anything
      }

      if (!playerTookATurn && entity == model.player) {
        playerTookATurn = true;
      }

      model.DrainEventQueue();
      OnStep?.Invoke(entity);

      bool shouldStagger =
        !isFirstIteration &&
        !(entity is INoTurnDelay) && 
        entity is Entity e &&
        e.isVisible &&
        (e is Actor || forceStaggerThisTurn);
      if (shouldStagger) {
        forceStaggerThisTurn = false;
        // stagger actors just a bit for juice
        yield return new WaitForSeconds(JUICE_STAGGER_SECONDS);
      }

      activeEntity = null;
      isFirstIteration = false;
    } while (true);
  }
}