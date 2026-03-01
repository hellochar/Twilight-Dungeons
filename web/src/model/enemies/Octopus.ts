import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackGroundTask } from '../tasks/AttackGroundTask';
import { ChaseTargetTask } from '../tasks/ChaseTargetTask';
import { RunAwayTask } from '../tasks/RunAwayTask';
import { WaitTask } from '../tasks/WaitTask';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Entity } from '../Entity';

/**
 * Attacks at range 2. Runs away if you get too close.
 * Port of C# Octopus.cs.
 */
export class Octopus extends AIActor {
  get turnPriority(): number {
    return this.task?.constructor.name === 'AttackGroundTask' ? 90 : super.turnPriority;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this.hp = this._baseMaxHp = 3;
  }

  baseAttackDamage(): [number, number] {
    return [1, 2];
  }

  /** Diamond distance (manhattan) <= 2 */
  static isInRange(octopus: Entity, target: Entity): boolean {
    return Vector2Int.manhattanDistance(target.pos, octopus.pos) <= 2;
  }

  protected getNextTask(): ActorTask {
    const player = GameModelRef.main.player;
    if (this.isNextTo(player)) {
      return new RunAwayTask(this, player.pos, 1, true);
    }
    if (this.canTargetPlayer()) {
      if (Octopus.isInRange(this, player)) {
        return new AttackGroundTask(this, player.pos, 1);
      }
      const chase = new ChaseTargetTask(this, player);
      chase.maxMoves = 1;
      return chase;
    }
    return new WaitTask(this, 1);
  }
}

entityRegistry.register('Octopus', Octopus);
