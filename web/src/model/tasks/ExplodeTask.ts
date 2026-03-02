import { DoOnceTask, TaskStage } from '../ActorTask';
import { WaitBaseAction, type BaseAction } from '../BaseAction';
import type { Actor } from '../Actor';

/**
 * ExplodeTask — signals one turn before explosion. Visual marker only.
 * Port of C# ExplodeTask from Boombug.cs (shared by Boombug + FungalSentinel).
 */
export class ExplodeTask extends DoOnceTask {
  get whenToCheckIsDone(): TaskStage {
    return TaskStage.Before;
  }

  constructor(actor: Actor) {
    super(actor);
  }

  protected getNextActionImpl(): BaseAction {
    return new WaitBaseAction(this.actor);
  }
}
