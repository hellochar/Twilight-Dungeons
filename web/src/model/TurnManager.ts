import { Faction } from '../core/types';
import { EventEmitter } from '../core/EventEmitter';
import { NoActionException, ActorDiedException } from './Actor';
import { CannotPerformActionException } from './BaseAction';
import type { GameModel } from './GameModel';
import type { ISteppable } from './Floor';

export interface GameEvent {
  type: 'move' | 'attack' | 'damage' | 'death' | 'heal' | 'statusAdd' | 'statusRemove' | 'spawn' | 'pickup' | 'step';
  entityId: string;
  data?: any;
  simultaneous?: boolean;
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
          throw e;
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
}
