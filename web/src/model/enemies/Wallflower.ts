import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { MoveToTargetTask } from '../tasks/MoveToTargetTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import { WaitTask } from '../tasks/WaitTask';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { Wall } from '../Tile';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Tile } from '../Tile';

/**
 * Chases you but must stay adjacent to walls.
 * Port of C# Wallflower.cs.
 */
export class Wallflower extends AIActor {
  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = this._baseMaxHp = 2;
  }

  baseAttackDamage(): [number, number] {
    return [1, 1];
  }

  /** A tile can be occupied by a wallflower only if it's occupiable AND adjacent to a wall. */
  static canOccupy(t: Tile): boolean {
    if (!t.canBeOccupied()) return false;
    const adjacent = t.floor!.getAdjacentTiles(t.pos);
    return adjacent.some(n => n instanceof Wall);
  }

  protected getNextTask(): ActorTask {
    const floor = this.floor!;
    const tethers = floor.getCardinalNeighbors(this.pos).filter(t => t instanceof Wall);

    if (tethers.length === 0) {
      // Not touching a wall — walk randomly until we are
      return new MoveRandomlyTask(this);
    }

    const player = GameModelRef.main.player;
    if (this.canTargetPlayer()) {
      if (this.isNextTo(player)) {
        return new AttackTask(this, player);
      }
    }

    // Find walls reachable via wall-adjacent-to-wall chain (one step out)
    const nextTethers = new Set<Tile>();
    for (const touchingWall of tethers) {
      for (const neighbor of floor.getCardinalNeighbors(touchingWall.pos, true)) {
        if (neighbor instanceof Wall) {
          nextTethers.add(neighbor);
        }
      }
    }

    // Candidate tiles: adjacent to me, wallflower-occupiable, and cardinally next to a nextTether
    const candidateTiles = floor
      .getAdjacentTiles(this.pos)
      .filter(t => Wallflower.canOccupy(t))
      .filter(adjacent =>
        floor.getCardinalNeighbors(adjacent.pos).some(n => nextTethers.has(n)),
      );

    // Pick tile closest to player
    const playerPos = GameModelRef.main.player.pos;
    const nextTile = candidateTiles.length > 0
      ? candidateTiles.reduce((best, t) =>
          Vector2Int.distance(t.pos, playerPos) < Vector2Int.distance(best.pos, playerPos) ? t : best,
        )
      : null;

    if (nextTile != null && nextTile !== this.tile) {
      return new MoveToTargetTask(this, nextTile.pos);
    }
    return new WaitTask(this, 1);
  }
}

entityRegistry.register('Wallflower', Wallflower);
