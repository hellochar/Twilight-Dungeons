import { ATTACK_DAMAGE_MOD, type IAttackDamageModifier } from '../../core/Modifiers';
import { StackingStatus } from '../Status';

/**
 * Reduces attack damage by 1 per stack. Stack decrements per attack.
 * Port of C# WeaknessStatus from Deathbloom.cs.
 */
export class WeaknessStatus extends StackingStatus implements IAttackDamageModifier {
  readonly [ATTACK_DAMAGE_MOD] = true as const;

  get isDebuff(): boolean {
    return true;
  }

  constructor(stacks = 1) {
    super(stacks);
  }

  /** Handles both STEP_MOD (inherited) and ATTACK_DAMAGE_MOD calls. */
  modify(input: any): any {
    if (typeof input !== 'number') {
      return super.modify(input);
    }
    this.stacks--;
    return input - 1;
  }
}
