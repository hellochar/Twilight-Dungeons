import { FollowPathTask } from './FollowPathTask';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import type { Actor } from '../Actor';
import type { Body } from '../Body';

export class MoveNextToTargetTask extends FollowPathTask {
  constructor(actor: Actor, target: Vector2Int) {
    super(actor, target, MoveNextToTargetTask.findBestAdjacentPath(actor, target));
  }

  static findBestAdjacentPath(body: Body, target: Vector2Int): Vector2Int[] {
    if (Vector2Int.equals(body.pos, target)) return [];
    const floor = GameModelRef.main.currentFloor;
    const path = floor.findPath(body.pos, target, true, body);
    if (path.length > 0) {
      path.pop(); // Remove the target itself — we want to be *next to* it
    }
    return path;
  }
}
