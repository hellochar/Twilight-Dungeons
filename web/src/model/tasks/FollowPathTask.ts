import { ActorTask, TaskStage } from '../ActorTask';
import { MoveBaseAction, type BaseAction } from '../BaseAction';
import { Vector2Int } from '../../core/Vector2Int';
import type { Actor } from '../Actor';

export class FollowPathTask extends ActorTask {
  private _target: Vector2Int;
  get target(): Vector2Int { return this._target; }
  path: Vector2Int[];
  maxMoves = Infinity;
  private _timesMoved = 0;

  get whenToCheckIsDone(): TaskStage {
    return TaskStage.After;
  }

  get timesMoved(): number {
    return this._timesMoved;
  }

  constructor(actor: Actor, target: Vector2Int, path: Vector2Int[]) {
    super(actor);
    this._target = target;
    this.path = path;
  }

  setMaxMoves(moves: number): this {
    this.maxMoves = moves;
    return this;
  }

  protected getNextActionImpl(): BaseAction {
    const nextPos = this.path.shift()!;
    this._timesMoved++;
    return new MoveBaseAction(this.actor, nextPos);
  }

  isDone(): boolean {
    return this.path.length === 0 || this._timesMoved >= this.maxMoves;
  }
}
