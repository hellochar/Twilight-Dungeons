import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { WaitTask } from '../tasks/WaitTask';
import { MoveToTargetTask } from '../tasks/MoveToTargetTask';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';

/**
 * Only moves/attacks if you're in the same row or column.
 * Attacks apply Weakness.
 * Port of Snake.cs.
 */
export class Snake extends AIActor {
  get turnPriority(): number {
    return 20;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this.hp = this._baseMaxHp = 3;
  }

  baseAttackDamage(): [number, number] {
    return [1, 1];
  }

  protected getNextTask(): ActorTask {
    const player = GameModelRef.main.player;
    if (this.canTargetPlayer() && (player.pos.x === this.pos.x || player.pos.y === this.pos.y)) {
      const dx = Math.sign(player.pos.x - this.pos.x);
      const dy = Math.sign(player.pos.y - this.pos.y);
      const direction = new Vector2Int(dx, dy);
      const nextPos = Vector2Int.add(this.pos, direction);

      const targetBody = this.floor?.bodies.get(nextPos);
      const tile = this.floor?.tiles.get(nextPos);

      if (tile && tile.canBeOccupied()) {
        return new MoveToTargetTask(this, nextPos);
      } else if (targetBody && targetBody !== this) {
        return new AttackTask(this, targetBody as any);
      }
    }
    return new WaitTask(this, 1);
  }

  /** Apply weakness on attack (stub — WeaknessStatus not yet ported). */
  handleDealAttackDamage(_damage: number, _target: any): void {
    // TODO: target.statuses.add(new WeaknessStatus(1));
  }
}
