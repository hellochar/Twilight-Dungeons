import { FollowPathTask } from './FollowPathTask';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import type { Actor } from '../Actor';
import type { BaseAction } from '../BaseAction';

export class MoveToTargetTask extends FollowPathTask {
  constructor(actor: Actor, target: Vector2Int) {
    const floor = GameModelRef.main.currentFloor;
    super(actor, target, floor.findPath(actor.pos, target, false, actor));
  }
}

export class MoveToTargetThenPerformTask extends MoveToTargetTask {
  private readonly then: () => void;

  constructor(actor: Actor, target: Vector2Int, then: () => void) {
    super(actor, target);
    this.then = then;
  }

  postStep(_action: BaseAction, _finalAction: BaseAction): void {
    if (this.isDone()) {
      this.then();
    }
  }
}
