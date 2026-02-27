import { StackingMode, StackingStatus } from '../Status';
import { BODY_MOVE_HANDLER, type IBodyMoveHandler } from '../Body';
import { GameModelRef } from '../GameModelRef';
import { FreeMoveStatus } from './FreeMoveStatus';
import type { Vector2Int } from '../../core/Vector2Int';

/**
 * Tracks consecutive moves on SoftGrass. At 2 moves, grants FreeMoveStatus.
 * Port of C# SoftGrassStatus from SoftGrass.cs.
 */
export class SoftGrassStatus extends StackingStatus implements IBodyMoveHandler {
  readonly [BODY_MOVE_HANDLER] = true as const;

  get stackingMode(): StackingMode {
    return StackingMode.Ignore;
  }

  constructor(stacks = 1) {
    super(stacks);
  }

  handleMove(newPos: Vector2Int, oldPos: Vector2Int): void {
    if (newPos.x !== oldPos.x || newPos.y !== oldPos.y) {
      // Check if the new position has SoftGrass (duck type)
      const grass = this.actor?.floor?.grasses.get(newPos);
      if (grass && grass.constructor.name === 'SoftGrass') {
        this.stacks = this.stacks + 1;
        if (this.stacks === 2) {
          GameModelRef.main.enqueuEvent(() => this.actor!.statuses.add(new FreeMoveStatus()));
        } else if (this.stacks > 2) {
          this.stacks = 1;
        }
      }
    }
  }

  Step(): void {
    // Remove if not standing on SoftGrass
    const grass = this.actor?.grass;
    if (!grass || grass.constructor.name !== 'SoftGrass') {
      this.Remove();
    }
  }
}
