import { DoOnceTask } from '../ActorTask';
import { MoveBaseAction, type BaseAction } from '../BaseAction';
import type { Actor } from '../Actor';
import { Vector2Int } from '../../core/Vector2Int';

/**
 * Instantly move to a target tile (teleport/jump).
 * Used by Bird and ItemBirdWings.
 */
export class JumpToTargetTask extends DoOnceTask {
  readonly target: Vector2Int;

  constructor(actor: Actor, target: Vector2Int) {
    super(actor);
    this.target = target;
  }

  protected getNextActionImpl(): BaseAction {
    return new MoveBaseAction(this.actor, this.target);
  }
}
