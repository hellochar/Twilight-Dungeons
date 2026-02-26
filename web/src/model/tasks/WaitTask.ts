import { ActorTask, TaskStage } from '../ActorTask';
import { WaitBaseAction, type BaseAction } from '../BaseAction';
import type { Actor } from '../Actor';

export class WaitTask extends ActorTask {
  private turns: number;

  get whenToCheckIsDone(): TaskStage {
    return TaskStage.After;
  }

  get turnsRemaining(): number {
    return this.turns;
  }

  constructor(actor: Actor, turns: number) {
    super(actor);
    this.turns = turns;
  }

  protected getNextActionImpl(): BaseAction {
    this.turns--;
    return new WaitBaseAction(this.actor);
  }

  isDone(): boolean {
    return this.turns <= 0;
  }
}
