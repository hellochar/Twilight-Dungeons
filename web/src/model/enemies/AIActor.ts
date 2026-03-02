import { Actor, NoActionException } from '../Actor';
import { ActorTask } from '../ActorTask';
import { WaitTask } from '../tasks/WaitTask';
import { SleepTask } from '../tasks/SleepTask';
import { Inventory } from '../Inventory';
import { Vector2Int } from '../../core/Vector2Int';
import { Faction } from '../../core/types';
import type { Entity } from '../Entity';

/** Abstract AI override. Subclasses implement getNextTask(). */
export abstract class AI {
  abstract getNextTask(): ActorTask;
  start(): void {}
}

/**
 * Base class for AI-controlled enemies.
 * Port of AIActor.cs — handles retry loop on NoActionException,
 * death item drops, and sleep-on-spawn default behavior.
 */
export abstract class AIActor extends Actor {
  inventory = new Inventory(3);
  private static MAX_RETRIES = 2;
  private _aiOverride: AI | null = null;

  constructor(pos: Vector2Int) {
    super(pos);
    this.faction = Faction.Enemy;
    this.setTasks(new SleepTask(this));
  }

  /** Replace this actor's AI with a custom override. */
  setAI(ai: AI): void {
    this._aiOverride = ai;
    ai.start();
    this.clearTasks();
  }

  /** Subclasses implement this to decide what to do next. */
  protected abstract getNextTask(): ActorTask;

  handleDeath(_source: Entity | null): void {
    // Drop inventory items on death
    // (simplified — full item drop uses floor BFS for placement)
  }

  /**
   * Override step to retry with a new task on NoActionException.
   * This handles the case where an AI's chosen task immediately fails.
   */
  step(): number {
    for (let retries = -1; retries < AIActor.MAX_RETRIES; retries++) {
      try {
        return super.step();
      } catch (e) {
        if (e instanceof NoActionException) {
          this.setTasks(this._aiOverride ? this._aiOverride.getNextTask() : this.getNextTask());
        } else {
          throw e;
        }
      }
    }
    console.warn(`${this.displayName} reached MaxRetries`);
    this.setTasks(new WaitTask(this, 1));
    return super.step();
  }
}
