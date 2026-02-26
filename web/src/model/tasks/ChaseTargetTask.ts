import { MoveNextToTargetTask } from './MoveNextToTargetTask';
import { TaskStage } from '../ActorTask';
import { Vector2Int } from '../../core/Vector2Int';
import type { Actor } from '../Actor';
import type { Body } from '../Body';
import type { BaseAction } from '../BaseAction';

export class ChaseTargetTask extends MoveNextToTargetTask {
  protected targetBody: Body;
  private extraMovesCutoff: number;

  get whenToCheckIsDone(): TaskStage {
    return TaskStage.Before;
  }

  get target(): Vector2Int {
    return this.targetBody.pos;
  }

  constructor(actor: Actor, targetBody: Body, extraMovesCutoff = 0) {
    super(actor, targetBody.pos);
    this.targetBody = targetBody;
    this.extraMovesCutoff = extraMovesCutoff;
  }

  getTargetBody(): Body {
    return this.targetBody;
  }

  preStep(): void {
    if (!this.targetBody || this.targetBody.isDead) {
      this.path = [];
    } else {
      this.path = MoveNextToTargetTask.findBestAdjacentPath(this.actor, this.targetBody.pos);
      if (this.extraMovesCutoff > 0 && this.path.length >= this.extraMovesCutoff) {
        this.path.splice(this.path.length - this.extraMovesCutoff, this.extraMovesCutoff);
      }
    }
  }
}
