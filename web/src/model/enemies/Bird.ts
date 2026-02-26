import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { WaitTask } from '../tasks/WaitTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import { JumpToTargetTask } from '../tasks/JumpToTargetTask';
import { Vector2Int } from '../../core/Vector2Int';
import { CollisionLayer } from '../../core/types';
import { GameModelRef } from '../GameModelRef';
import type { Tile } from '../Tile';
import type { Entity } from '../Entity';

/**
 * Jumps two tiles per turn and waits after every jump.
 * Port of Bird.cs.
 */
export class Bird extends AIActor {
  get baseMovementLayer(): CollisionLayer {
    return CollisionLayer.Flying;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this.hp = this._baseMaxHp = 3;
  }

  baseAttackDamage(): [number, number] {
    return [1, 2];
  }

  /** Get tiles this entity can jump to (diamond distance == 2). */
  static getJumpTiles(e: Entity): Tile[] {
    if (!e.floor) return [];
    const results: Tile[] = [];
    for (const pos of e.floor.enumerateCircle(e.pos, 3)) {
      const dx = Math.abs(pos.x - e.pos.x);
      const dy = Math.abs(pos.y - e.pos.y);
      if (dx + dy !== 2) continue;
      const tile = e.floor.tiles.get(pos);
      if (!tile) continue;
      if ('hp' in e) {
        if (tile.canBeOccupiedBy(e as any)) results.push(tile);
      } else {
        if (tile.canBeOccupied()) results.push(tile);
      }
    }
    return results;
  }

  protected getNextTask(): ActorTask {
    const player = GameModelRef.main.player;
    if (this.canTargetPlayer()) {
      if (this.isNextTo(player)) {
        return new AttackTask(this, player);
      }
      // Jump to closest tile to player
      const jumpTiles = Bird.getJumpTiles(this);
      if (jumpTiles.length > 0) {
        jumpTiles.sort((a, b) => a.distanceTo(player) - b.distanceTo(player));
        return new JumpToTargetTask(this, jumpTiles[0].pos);
      }
      return new WaitTask(this, 1);
    }
    return new MoveRandomlyTask(this);
  }

  /** After moving, insert a wait turn. */
  handleActionPerformed(final_: any, _initial: any): void {
    if (final_.type === 'move') {
      this.insertTasks(new WaitTask(this, 1));
    }
  }
}
