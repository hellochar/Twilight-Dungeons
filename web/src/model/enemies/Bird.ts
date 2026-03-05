import { AIActor } from './AIActor';
import { ACTION_PERFORMED_HANDLER, type IActionPerformedHandler } from '../Actor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { WaitTask } from '../tasks/WaitTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import { JumpToTargetTask } from '../tasks/JumpToTargetTask';
import { Vector2Int } from '../../core/Vector2Int';
import { CollisionLayer, ActionType } from '../../core/types';
import { GameModelRef } from '../GameModelRef';
import type { BaseAction } from '../BaseAction';
import type { Tile } from '../Tile';
import type { Entity } from '../Entity';
import { entityRegistry } from '../../generator/entityRegistry';

/**
 * Jumps two tiles per turn and waits after every jump.
 * Port of Bird.cs.
 */
export class Bird extends AIActor implements IActionPerformedHandler {
  readonly [ACTION_PERFORMED_HANDLER] = true;

  get baseMovementLayer(): CollisionLayer {
    return CollisionLayer.Flying;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this.hp = this._baseMaxHp = 2;
  }

  baseAttackDamage(): [number, number] {
    return [1, 1];
  }

  /** After moving, insert a wait turn. */
  handleActionPerformed(final_: BaseAction, _initial: BaseAction): void {
    if (final_.type === ActionType.MOVE) {
      this.insertTasks(new WaitTask(this, 1));
    }
  }

  /** Get tiles this entity can jump to (Manhattan distance == 2). */
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
}

entityRegistry.register('Bird', Bird);
