import { ATTACK_DAMAGE_TAKEN_MOD, type IAttackDamageTakenModifier } from '../../core/Modifiers';
import { StackingStatus } from '../Status';

/**
 * Blocks 1 attack damage per stack, consuming a stack each time.
 * Port of C# ArmoredStatus from Frizzlefen.cs.
 */
export class ArmoredStatus extends StackingStatus implements IAttackDamageTakenModifier {
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;

  constructor(stacks = 1) {
    super(stacks);
  }

  modify(input: any): any {
    if (typeof input === 'number') {
      if (input > 0) {
        this.stacks--;
        return input - 1;
      }
      return input;
    }
    return super.modify(input);
  }
}
