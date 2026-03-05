import { ATTACK_DAMAGE_MOD, type IAttackDamageModifier } from '../../core/Modifiers';
import { StackingStatus } from '../Status';
import { WeaknessStatus } from './WeaknessStatus';

/**
 * +2 attack damage while active. On removal, applies WeaknessStatus(3).
 * Port of C# FrenziedStatus from Deathbloom.cs.
 */
export class FrenziedStatus extends StackingStatus implements IAttackDamageModifier {
  readonly [ATTACK_DAMAGE_MOD] = true as const;

  constructor(stacks: number) {
    super(stacks);
  }

  modify(input: any): any {
    if (typeof input === 'number') {
      this.stacks -= 1;
      return input + 2;
    }
    return super.modify(input);
  }

  End(): void {
    this.actor?.statuses.add(new WeaknessStatus(3));
  }
}
