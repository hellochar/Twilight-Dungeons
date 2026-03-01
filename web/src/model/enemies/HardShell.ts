import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { ChaseTargetTask } from '../tasks/ChaseTargetTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import {
  ATTACK_DAMAGE_TAKEN_MOD,
  type IAttackDamageTakenModifier,
} from '../../core/Modifiers';
import { entityRegistry } from '../../generator/entityRegistry';

/**
 * If HardShell would take 3+ attack damage, it is reduced to 0.
 * Port of C# HardShell.cs.
 */
export class HardShell extends AIActor implements IAttackDamageTakenModifier {
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;

  get turnPriority(): number {
    return 50;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = this._baseMaxHp = 8;
  }

  baseAttackDamage(): [number, number] {
    return [2, 2];
  }

  modify(input: number): number {
    if (input >= 3) {
      return 0;
    }
    return input;
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

entityRegistry.register('HardShell', HardShell);
