import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { ChaseTargetTask } from '../tasks/ChaseTargetTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import { SurprisedStatus } from '../tasks/SleepTask';
import { BODY_TAKE_ATTACK_DAMAGE_HANDLER, type IBodyTakeAttackDamageHandler } from '../Body';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Actor } from '../Actor';

/**
 * When attacked, duplicates into two goos with half HP each.
 * Port of C# Goo.cs.
 */
export class Goo extends AIActor implements IBodyTakeAttackDamageHandler {
  readonly [BODY_TAKE_ATTACK_DAMAGE_HANDLER] = true as const;

  get turnPriority(): number {
    return 50;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this.hp = this._baseMaxHp = 8;
  }

  baseAttackDamage(): [number, number] {
    return [1, 1];
  }

  handleTakeAttackDamage(damage: number, _hpBefore: number, _source: Actor): void {
    if (damage > 0) {
      GameModelRef.main.enqueuEvent(() => this.split());
    }
  }

  private split(): void {
    if (this.hp < 2) return;

    const hp1 = Math.ceil(this.hp / 2);
    const hp2 = Math.floor(this.hp / 2);

    const goo1 = new Goo(this.pos);
    goo1.hp = hp1;
    goo1.clearTasks();
    goo1.statuses.add(new SurprisedStatus());

    const goo2 = new Goo(this.pos);
    goo2.hp = hp2;
    goo2.clearTasks();
    goo2.statuses.add(new SurprisedStatus());

    this.floor!.putAll([goo1, goo2]);
    this.floor!.remove(this);
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

entityRegistry.register('Goo', Goo);
