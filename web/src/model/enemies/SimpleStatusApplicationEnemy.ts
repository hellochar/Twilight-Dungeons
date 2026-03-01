import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { WaitTask } from '../tasks/WaitTask';
import { TelegraphedTask } from '../tasks/TelegraphedTask';
import { GenericTask } from '../tasks/GenericTask';
import { GenericBaseAction } from '../BaseAction';
import { Vector2Int } from '../../core/Vector2Int';
import { Faction } from '../../core/types';

/**
 * Abstract base for enemies that perform status effects on a cooldown cycle.
 * Alternates: wait (cooldown turns) → telegraph/perform action → repeat.
 * Port of C# SimpleStatusApplicationEnemy.
 */
export abstract class SimpleStatusApplicationEnemy extends AIActor {
  get cooldown(): number { return 9; }
  get telegraphs(): boolean { return true; }

  private justWaited = false;

  constructor(pos: Vector2Int) {
    super(pos);
    this.faction = Faction.Enemy;
    this.clearTasks();
    this._hp = 2;
    this._baseMaxHp = 2;
  }

  /** Override to add extra conditions. */
  filter(): boolean { return true; }

  protected getNextTask(): ActorTask {
    if (this.justWaited) {
      if (this.canTargetPlayer() && this.filter()) {
        if (this.telegraphs) {
          return new TelegraphedTask(this, 1, new GenericBaseAction(this, () => this.tryDoTask()));
        } else {
          return new GenericTask(this, () => this.tryDoTask());
        }
      } else {
        return new WaitTask(this, 1);
      }
    } else {
      this.justWaited = true;
      return new WaitTask(this, this.cooldown);
    }
  }

  tryDoTask(): void {
    if (this.canTargetPlayer()) {
      this.justWaited = false;
      this.doTask();
    }
  }

  /** Subclasses implement the actual status/effect application. */
  abstract doTask(): void;

  baseAttackDamage(): [number, number] {
    return [0, 0];
  }
}
