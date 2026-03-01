import { StackingStatus } from '../Status';
import {
  ATTACK_DAMAGE_TAKEN_MOD,
  type IAttackDamageTakenModifier,
} from '../../core/Modifiers';

/**
 * Take 1 more attack damage per stack. Loses 1 stack per turn.
 * Port of C# VulnerableStatus.
 */
export class VulnerableStatus extends StackingStatus implements IAttackDamageTakenModifier {
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;

  get isDebuff(): boolean {
    return true;
  }

  constructor(stacks: number) {
    super(stacks);
  }

  Step(): void {
    this.stacks--;
  }

  modify(input: any): any {
    if (typeof input === 'number') {
      return input + 1;
    }
    return super.modify(input);
  }
}
