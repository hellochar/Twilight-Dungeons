import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { ChaseTargetTask } from '../tasks/ChaseTargetTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import { WaitTask } from '../tasks/WaitTask';
import { InShellStatus } from '../statuses/InShellStatus';
import { ACTION_PERFORMED_HANDLER, type IActionPerformedHandler } from '../Actor';
import { TAKE_ANY_DAMAGE_HANDLER, type ITakeAnyDamageHandler } from '../Body';
import { ActionType } from '../../core/types';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { entityRegistry } from '../../generator/entityRegistry';
import type { BaseAction } from '../BaseAction';

/**
 * Goes into shell when damaged. Pauses after each non-wait action.
 * Port of C# Snail.cs.
 */
export class Snail extends AIActor implements IActionPerformedHandler, ITakeAnyDamageHandler {
  readonly [ACTION_PERFORMED_HANDLER] = true as const;
  readonly [TAKE_ANY_DAMAGE_HANDLER] = true as const;

  constructor(pos: Vector2Int) {
    super(pos);
    this.hp = this._baseMaxHp = 3;
  }

  baseAttackDamage(): [number, number] {
    return [2, 2];
  }

  handleActionPerformed(finalAction: BaseAction, _initialAction: BaseAction): void {
    if (finalAction.type !== ActionType.WAIT) {
      this.insertTasks(new WaitTask(this, 1));
    }
  }

  handleTakeAnyDamage(_dmg: number): void {
    this.statuses.add(new InShellStatus());
  }

  protected getNextTask(): ActorTask {
    const player = GameModelRef.main.player;
    if (this.canTargetPlayer()) {
      if (this.isNextTo(player)) {
        return new AttackTask(this, player);
      }
      return new ChaseTargetTask(this, player);
    }
    return new MoveRandomlyTask(this);
  }
}

entityRegistry.register('Snail', Snail);
