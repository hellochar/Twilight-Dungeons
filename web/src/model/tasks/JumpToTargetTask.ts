import { DoOnceTask } from '../ActorTask';
import { JumpBaseAction, type BaseAction } from '../BaseAction';
import type { Actor } from '../Actor';
import { Vector2Int } from '../../core/Vector2Int';

/**
 * Jump to a target tile with parabolic arc animation.
 * Used by Bird and ItemBirdWings.
 */
export class JumpToTargetTask extends DoOnceTask {
  readonly target: Vector2Int;

  constructor(actor: Actor, target: Vector2Int) {
    super(actor);
    this.target = target;
  }

  protected getNextActionImpl(): BaseAction {
    return new JumpBaseAction(this.actor, this.target);
  }
}
