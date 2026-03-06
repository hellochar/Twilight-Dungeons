import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { ChaseTargetTask } from '../tasks/ChaseTargetTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import { ActionCosts } from '../BaseAction';
import { ActionType } from '../../core/types';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { entityRegistry } from '../../generator/entityRegistry';

/**
 * Fast enemy: moves and attacks at half cost (acts twice per turn).
 * Port of C# Scorpion.cs.
 */
export class Scorpion extends AIActor {
  protected get actionCosts(): ActionCosts {
    return new ActionCosts([
      [ActionType.MOVE, 0.5],
      [ActionType.ATTACK, 0.5],
      [ActionType.WAIT, 1],
      [ActionType.GENERIC, 1],
    ]);
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this.hp = this._baseMaxHp = 2;
  }

  baseAttackDamage(): [number, number] {
    return [1, 1];
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

entityRegistry.register('Scorpion', Scorpion);
