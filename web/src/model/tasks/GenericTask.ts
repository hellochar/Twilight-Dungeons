import { DoOnceTask } from '../ActorTask';
import { GenericBaseAction, type BaseAction } from '../BaseAction';
import type { Actor } from '../Actor';
import type { Player } from '../Player';

export class GenericTask extends DoOnceTask {
  readonly action: () => void;

  constructor(actor: Actor, action: () => void) {
    super(actor);
    this.action = action;
  }

  protected getNextActionImpl(): BaseAction {
    return new GenericBaseAction(this.actor, this.action);
  }
}

export class GenericPlayerTask extends GenericTask {
  constructor(actor: Player, action: () => void) {
    super(actor, action);
  }
}
