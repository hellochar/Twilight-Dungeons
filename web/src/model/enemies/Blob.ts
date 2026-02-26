import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackGroundTask } from '../tasks/AttackGroundTask';
import { ChaseTargetTask } from '../tasks/ChaseTargetTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';

/**
 * Telegraphs attacks for 1 turn. Chases you.
 * Port of Blob.cs.
 */
export class Blob extends AIActor {
  get turnPriority(): number {
    return this.task?.constructor.name === 'AttackGroundTask' ? 90 : super.turnPriority;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this.hp = this._baseMaxHp = 6;
  }

  baseAttackDamage(): [number, number] {
    return [2, 3];
  }

  protected getNextTask(): ActorTask {
    const player = GameModelRef.main.player;
    if (this.canTargetPlayer()) {
      if (this.isNextTo(player)) {
        return new AttackGroundTask(this, player.pos, 1);
      } else {
        return new ChaseTargetTask(this, player);
      }
    }
    return new MoveRandomlyTask(this);
  }
}

/**
 * Smaller version of Blob. Same behavior, less HP/damage.
 */
export class MiniBlob extends AIActor {
  get turnPriority(): number {
    return this.task?.constructor.name === 'AttackGroundTask' ? 90 : super.turnPriority;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this.hp = this._baseMaxHp = 3;
  }

  baseAttackDamage(): [number, number] {
    return [2, 2];
  }

  protected getNextTask(): ActorTask {
    const player = GameModelRef.main.player;
    if (this.canTargetPlayer()) {
      if (this.isNextTo(player)) {
        return new AttackGroundTask(this, player.pos, 1);
      } else {
        return new ChaseTargetTask(this, player);
      }
    }
    return new MoveRandomlyTask(this);
  }
}
