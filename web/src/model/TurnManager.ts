import { Faction } from '../core/types';
import { EventEmitter } from '../core/EventEmitter';
import { NoActionException, ActorDiedException, Actor } from './Actor';
import { CannotPerformActionException } from './BaseAction';
import type { GameModel } from './GameModel';
import type { ISteppable } from './Floor';
import { Entity } from './Entity';
import { WaitTask } from './tasks/WaitTask';
import { FollowPathTask } from './tasks/FollowPathTask';

/** Result of stepping a single entity. */
export interface StepResult {
  done: boolean;
  entity: ISteppable | null;
  /** Game-time gap before this entity's turn. */
  timeGap: number;
  isFirstStep: boolean;
  /** True if this step should have a visual stagger delay. */
  shouldStagger: boolean;
  /** True if player is doing a long wait/walk and delays should be skipped. */
  shouldSpeedThrough: boolean;
}

/**
 * Synchronous turn manager — no coroutines, no yields.
 * Runs all entities until it's the player's choice again.
 * Port of C# TurnManager.cs.
 */
export class TurnManager {
  private model: GameModel;
  activeEntity: ISteppable | null = null;

  readonly onPlayersChoice = new EventEmitter();
  readonly onStep = new EventEmitter<[ISteppable]>();
  readonly onTimePassed = new EventEmitter();
  readonly onPlayerCannotPerform = new EventEmitter<[CannotPerformActionException]>();

  // ─── Incremental session state ───
  private _sessionActive = false;
  private _isFirstIteration = true;
  private _guard = 0;
  private _playerTookATurn = false;
  private _enemyTookATurn = false;

  constructor(model: GameModel) {
    this.model = model;
  }

  private findActiveEntity(): ISteppable {
    const entities = this.model.getAllEntitiesInPlay();
    return entities.reduce((best, current) => {
      if (current.timeNextAction === best.timeNextAction) {
        return current.turnPriority < best.turnPriority ? current : best;
      }
      return current.timeNextAction < best.timeNextAction ? current : best;
    });
  }

  /**
   * Step all entities synchronously until it's the player's turn to choose.
   * This replaces the C# IEnumerator coroutine.
   */
  stepUntilPlayerChoice(): void {
    try {
      this.stepUntilPlayerChoiceImpl();
    } finally {
      this.onPlayersChoice.emit();
    }
  }

  private stepUntilPlayerChoiceImpl(): void {
    if (this.model.player.isDead) return;

    this.model.drainEventQueue();

    let guard = 0;
    let playerTookATurn = false;
    let enemyTookATurn = false;

    while (true) {
      if (guard++ > 1000 && !this.model.player.isDead) {
        console.warn('Stopping step: 1000 turns since player had a turn');
        break;
      }

      const entity = this.findActiveEntity();
      this.activeEntity = entity;

      if (this.model.time > entity.timeNextAction) {
        console.error(`time is ${this.model.time} but ${entity} had a turn at ${entity.timeNextAction}`);
        entity.timeNextAction = this.model.time;
      }

      if (this.model.time !== entity.timeNextAction) {
        this.model.time = entity.timeNextAction;
        this.onTimePassed.emit();

        // Fire timed events
        let evt = this.model.timedEvents.next();
        while (evt && evt.time <= this.model.time) {
          if (evt.owner.floor !== this.model.currentFloor) {
            this.model.timedEvents.unregister(evt);
          } else {
            evt.action();
            this.model.timedEvents.unregister(evt);
            this.model.drainEventQueue();
          }
          evt = this.model.timedEvents.next();
        }
      }

      // Check if player needs to make a choice
      if (entity === this.model.player) {
        const noMoreTasks = this.model.player.task === null;
        const worldHasChanged = playerTookATurn && enemyTookATurn;
        if (noMoreTasks || worldHasChanged) break;
      } else if ('faction' in entity && (entity as any).faction === Faction.Enemy) {
        enemyTookATurn = true;
      }

      // Do the step
      try {
        const cost = entity.step();
        entity.timeNextAction = this.model.time + cost;
      } catch (e) {
        if (e instanceof NoActionException) {
          if (entity === this.model.player) {
            break;
          } else {
            entity.timeNextAction += 1;
            console.warn(`${entity} NoActionException`);
          }
        } else if (e instanceof CannotPerformActionException) {
          if (entity === this.model.player) {
            this.onPlayerCannotPerform.emit(e);
            break;
          } else {
            entity.timeNextAction += 1;
          }
        } else if (e instanceof ActorDiedException) {
          // Just continue
        } else {
          console.error(`Unexpected error during step for ${entity}:`, e);
          entity.timeNextAction = this.model.time + 1;
        }
      }

      if (!playerTookATurn && entity === this.model.player) {
        playerTookATurn = true;
      }

      this.model.drainEventQueue();
      this.onStep.emit(entity);
      this.activeEntity = null;
    }
  }

  // ─── Incremental stepping API ───

  /** Begin a new incremental step session. Call stepOneEntity() repeatedly after this. */
  beginStepSession(): void {
    if (this.model.player.isDead) {
      this._sessionActive = false;
      return;
    }
    this.model.drainEventQueue();
    this._sessionActive = true;
    this._isFirstIteration = true;
    this._guard = 0;
    this._playerTookATurn = false;
    this._enemyTookATurn = false;
  }

  /** Execute exactly one entity's turn. Returns metadata about the step. */
  stepOneEntity(): StepResult {
    const DONE: StepResult = {
      done: true, entity: null, timeGap: 0,
      isFirstStep: false, shouldStagger: false, shouldSpeedThrough: false,
    };

    if (!this._sessionActive || this.model.player.isDead) {
      this._sessionActive = false;
      this.onPlayersChoice.emit();
      return DONE;
    }

    if (this._guard++ > 1000) {
      console.warn('Stopping step: 1000 turns since player had a turn');
      this._sessionActive = false;
      this.onPlayersChoice.emit();
      return DONE;
    }

    const entity = this.findActiveEntity();
    this.activeEntity = entity;
    const isFirstStep = this._isFirstIteration;

    // Compute time gap
    let timeGap = 0;
    if (this.model.time > entity.timeNextAction) {
      console.error(`time is ${this.model.time} but ${entity} had a turn at ${entity.timeNextAction}`);
      entity.timeNextAction = this.model.time;
    }

    if (this.model.time !== entity.timeNextAction) {
      timeGap = entity.timeNextAction - this.model.time;
      this.model.time = entity.timeNextAction;
      this.onTimePassed.emit();

      // Fire timed events
      let evt = this.model.timedEvents.next();
      while (evt && evt.time <= this.model.time) {
        if (evt.owner.floor !== this.model.currentFloor) {
          this.model.timedEvents.unregister(evt);
        } else {
          evt.action();
          this.model.timedEvents.unregister(evt);
          this.model.drainEventQueue();
        }
        evt = this.model.timedEvents.next();
      }
    }

    // Check if player needs to make a choice (before stepping)
    if (entity === this.model.player) {
      const noMoreTasks = this.model.player.task === null;
      const worldHasChanged = this._playerTookATurn && this._enemyTookATurn;
      if (noMoreTasks || worldHasChanged) {
        this._sessionActive = false;
        this.onPlayersChoice.emit();
        return DONE;
      }
    } else if ('faction' in entity && (entity as any).faction === Faction.Enemy) {
      this._enemyTookATurn = true;
    }

    // Speed-through detection (matches C# TurnManager lines 102-103):
    // WaitTask turns > 3, or FollowPathTask path.length > 10
    let shouldSpeedThrough = false;
    const player = this.model.player;
    if (player.task instanceof WaitTask && player.task.turnsRemaining > 3) {
      shouldSpeedThrough = true;
    } else if (player.task instanceof FollowPathTask && player.task.path.length > 10) {
      shouldSpeedThrough = true;
    }

    // Stagger logic (matches C# TurnManager lines 167-177):
    // !isFirstIteration && !(entity has noTurnDelay) && entity is Entity && entity.isVisible && entity is Actor
    const isNoTurnDelay = 'noTurnDelay' in entity && (entity as any).noTurnDelay === true;
    const isEntity = entity instanceof Entity;
    const isActor = entity instanceof Actor;
    const entityVisible = isEntity && entity.isVisible;
    const shouldStagger = !this._isFirstIteration && !isNoTurnDelay && entityVisible && isActor;

    // Do the step
    try {
      const cost = entity.step();
      entity.timeNextAction = this.model.time + cost;
    } catch (e) {
      if (e instanceof NoActionException) {
        if (entity === this.model.player) {
          this._sessionActive = false;
          this.onPlayersChoice.emit();
          return { done: true, entity, timeGap, isFirstStep, shouldStagger: false, shouldSpeedThrough };
        } else {
          entity.timeNextAction += 1;
          console.warn(`${entity} NoActionException`);
        }
      } else if (e instanceof CannotPerformActionException) {
        if (entity === this.model.player) {
          this.onPlayerCannotPerform.emit(e);
          this._sessionActive = false;
          this.onPlayersChoice.emit();
          return { done: true, entity, timeGap, isFirstStep, shouldStagger: false, shouldSpeedThrough };
        } else {
          entity.timeNextAction += 1;
        }
      } else if (e instanceof ActorDiedException) {
        // Just continue
      } else {
        console.error(`Unexpected error during step for ${entity}:`, e);
        entity.timeNextAction = this.model.time + 1;
      }
    }

    if (!this._playerTookATurn && entity === this.model.player) {
      this._playerTookATurn = true;
    }

    this.model.drainEventQueue();
    this.onStep.emit(entity);
    this.activeEntity = null;
    this._isFirstIteration = false;

    return {
      done: false,
      entity,
      timeGap,
      isFirstStep,
      shouldStagger,
      shouldSpeedThrough,
    };
  }
}
