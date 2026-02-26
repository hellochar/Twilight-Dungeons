import type { BaseAction } from './BaseAction';
import type { Actor } from './Actor';

export enum TaskStage {
  Before = 1,
  After = 2,
}

/**
 * Base class for all actor tasks.
 * Port of C# ActorTask.cs.
 */
export abstract class ActorTask {
  readonly actor: Actor;
  name: string;
  private _forceOnlyCheckBefore = false;
  private _isFreeTask = false;

  get isFreeTask(): boolean {
    return this._isFreeTask;
  }

  get whenToCheckIsDone(): TaskStage {
    return TaskStage.Before;
  }

  get forceOnlyCheckBefore(): boolean {
    return this._forceOnlyCheckBefore;
  }

  get isPlayerOverridable(): boolean {
    return true;
  }

  constructor(actor: Actor) {
    this.actor = actor;
    this.name = this.constructor.name.replace('Task', '');
  }

  named(name: string): this {
    this.name = name;
    return this;
  }

  onlyCheckBefore(): this {
    this._forceOnlyCheckBefore = true;
    return this;
  }

  free(): this {
    this._isFreeTask = true;
    return this;
  }

  preStep(): void {}
  postStep(_action: BaseAction, _finalAction: BaseAction): void {}

  getNextAction(): BaseAction {
    return this.getNextActionImpl();
  }

  abstract isDone(): boolean;
  protected abstract getNextActionImpl(): BaseAction;

  ended(): void {}
}

/**
 * A task that runs exactly once, then is done.
 */
export abstract class DoOnceTask extends ActorTask {
  private hasDoneOnce = false;

  get whenToCheckIsDone(): TaskStage {
    return TaskStage.After;
  }

  getNextAction(): BaseAction {
    this.hasDoneOnce = true;
    return super.getNextAction();
  }

  isDone(): boolean {
    return this.hasDoneOnce;
  }
}
