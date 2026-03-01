import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { ChaseTargetTask } from '../tasks/ChaseTargetTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import { WaitTask } from '../tasks/WaitTask';
import { SurprisedStatus } from '../tasks/SleepTask';
import { VulnerableStatus } from '../statuses/VulnerableStatus';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { ATTACK_HANDLER, type IAttackHandler } from '../Actor';
import { BODY_TAKE_ATTACK_DAMAGE_HANDLER, type IBodyTakeAttackDamageHandler } from '../Body';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Body } from '../Body';
import type { Actor } from '../Actor';

/**
 * Ghost — applies Vulnerable on hit, gets stunned when attacked.
 * Port of C# Dizapper.cs.
 */
export class Dizapper extends AIActor implements IAttackHandler, IBodyTakeAttackDamageHandler {
  readonly [ATTACK_HANDLER] = true as const;
  readonly [BODY_TAKE_ATTACK_DAMAGE_HANDLER] = true as const;

  get displayName(): string {
    return 'Ghost';
  }

  get turnPriority(): number {
    return 60;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = this._baseMaxHp = 9;
  }

  baseAttackDamage(): [number, number] {
    return [1, 1];
  }

  onAttack(_damage: number, target: Body): void {
    if ('statuses' in target) {
      (target as Actor).statuses.add(new VulnerableStatus(10));
    }
  }

  handleTakeAttackDamage(damage: number, _hp: number, _source: Actor): void {
    if (damage > 0) {
      this.setTasks(new WaitTask(this, 1));
      this.statuses.add(new SurprisedStatus());
    }
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

entityRegistry.register('Dizapper', Dizapper);
