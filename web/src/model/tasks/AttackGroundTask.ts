import { TelegraphedTask } from './TelegraphedTask';
import { AttackGroundBaseAction } from '../BaseAction';
import { Vector2Int } from '../../core/Vector2Int';
import type { Actor } from '../Actor';

export class AttackGroundTask extends TelegraphedTask {
  readonly targetPosition: Vector2Int;

  constructor(actor: Actor, targetPosition: Vector2Int, turns = 0) {
    super(actor, turns, new AttackGroundBaseAction(actor, targetPosition));
    this.targetPosition = targetPosition;
  }
}
