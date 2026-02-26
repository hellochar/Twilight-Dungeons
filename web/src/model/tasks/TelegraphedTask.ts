import { ActorTask, TaskStage } from '../ActorTask';
import { WaitBaseAction, type BaseAction } from '../BaseAction';
import { ActionType } from '../../core/types';
import type { Actor } from '../Actor';

export class TelegraphedTask extends ActorTask {
  private turns: number;
  private readonly actionType: ActionType;
  then: BaseAction;
  private done = false;

  get whenToCheckIsDone(): TaskStage {
    return TaskStage.After;
  }

  constructor(actor: Actor, turns: number, then: BaseAction, actionType?: ActionType) {
    super(actor);
    this.turns = turns;
    this.then = then;
    this.actionType = actionType ?? then.type;
  }

  protected getNextActionImpl(): BaseAction {
    if (this.turns > 0) {
      this.turns--;
      return new WaitBaseAction(this.actor, this.actionType);
    }
    this.done = true;
    return this.then;
  }

  postStep(action: BaseAction, finalAction: BaseAction): void {
    if (action !== finalAction) {
      this.done = true;
    }
  }

  isDone(): boolean {
    return this.done;
  }
}
