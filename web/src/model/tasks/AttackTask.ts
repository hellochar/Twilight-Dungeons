import { DoOnceTask } from '../ActorTask';
import { AttackBaseAction, type BaseAction } from '../BaseAction';
import type { Actor } from '../Actor';
import type { Body } from '../Body';

export class AttackTask extends DoOnceTask {
  readonly target: Body;

  constructor(actor: Actor, target: Body) {
    super(actor);
    this.target = target;
  }

  protected getNextActionImpl(): BaseAction {
    return new AttackBaseAction(this.actor, this.target);
  }
}
