import { StackingStatus } from '../Status';
import {
  ATTACK_DAMAGE_MOD,
  type IAttackDamageModifier,
} from '../../core/Modifiers';

/**
 * Next N attacks deal +1 damage. Consumes 1 stack per attack.
 * Port of C# StrengthStatus.
 */
export class StrengthStatus extends StackingStatus implements IAttackDamageModifier {
  readonly [ATTACK_DAMAGE_MOD] = true as const;

  constructor(stacks: number) {
    super(stacks);
  }

  modify(input: any): any {
    if (typeof input === 'number') {
      this.stacks--;
      return input + 1;
    }
    return super.modify(input);
  }
}
