import { StackingStatus } from '../Status';
import { BODY_TAKE_ATTACK_DAMAGE_HANDLER, type IBodyTakeAttackDamageHandler } from '../Body';
import { ATTACK_HANDLER, type IAttackHandler } from '../Actor';
import type { Body } from '../Body';

/**
 * Gained from Dandypuff. Cleared when taking damage.
 * OnAttack handler is a no-op (commented out in C#).
 * Port of C# DandyStatus from Dandypuff.cs.
 */
export class DandyStatus extends StackingStatus implements IBodyTakeAttackDamageHandler, IAttackHandler {
  readonly [BODY_TAKE_ATTACK_DAMAGE_HANDLER] = true as const;
  readonly [ATTACK_HANDLER] = true as const;

  constructor(stacks = 1) {
    super(stacks);
  }

  handleTakeAttackDamage(damage: number, _hp: number, _source: any): void {
    if (damage > 0) {
      this.stacks = 0;
    }
  }

  onAttack(_damage: number, _target: Body): void {
    // no-op (commented out in C#)
  }
}
