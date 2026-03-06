import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { ChaseDynamicTargetTask } from '../tasks/ChaseDynamicTargetTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import { SleepTask } from '../tasks/SleepTask';
import { ACTION_PERFORMED_HANDLER, DEAL_ATTACK_DAMAGE_HANDLER, type IActionPerformedHandler, type IDealAttackDamageHandler } from '../Actor';
import { CollisionLayer } from '../../core/types';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { entityRegistry } from '../../generator/entityRegistry';
import type { BaseAction } from '../BaseAction';
import type { Body } from '../Body';
import type { Actor } from '../Actor';

/**
 * Flying enemy. Targets nearest creature. Heals on damage dealt.
 * Goes into deep sleep after 5 turns awake.
 * Port of C# Bat.cs.
 */
export class Bat extends AIActor implements IActionPerformedHandler, IDealAttackDamageHandler {
  readonly [ACTION_PERFORMED_HANDLER] = true as const;
  readonly [DEAL_ATTACK_DAMAGE_HANDLER] = true as const;

  get baseMovementLayer(): CollisionLayer {
    return CollisionLayer.Flying;
  }

  private turnsUntilSleep = 5;

  constructor(pos: Vector2Int) {
    super(pos);
    this.hp = this._baseMaxHp = 3;
  }

  baseAttackDamage(): [number, number] {
    return [1, 1];
  }

  handleActionPerformed(_final: BaseAction, _initial: BaseAction): void {
    if (!(this.task instanceof SleepTask)) {
      this.turnsUntilSleep--;
      if (this.turnsUntilSleep <= 0) {
        this.setTasks(new SleepTask(this, 5, true));
      }
    }
  }

  protected taskChanged(): void {
    if (this.task instanceof SleepTask) {
      this.turnsUntilSleep = 5;
    }
    super.taskChanged();
  }

  handleDealAttackDamage(dmg: number, target: Body): void {
    if (target !== this && (target as any).faction !== undefined && dmg > 0) {
      this.heal(1);
    }
  }

  protected getNextTask(): ActorTask {
    const target = this.selectTarget();
    if (!target) {
      return new MoveRandomlyTask(this);
    }
    if (this.isNextTo(target)) {
      return new AttackTask(this, target);
    }
    return new ChaseDynamicTargetTask(this, () => this.selectTarget()!);
  }

  private selectTarget(): Actor | null {
    const player = GameModelRef.main.player;
    if (this.canTargetPlayer()) {
      return player;
    }
    return null;
  }
}

entityRegistry.register('Bat', Bat);
