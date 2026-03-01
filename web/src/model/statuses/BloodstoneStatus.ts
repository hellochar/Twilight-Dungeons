import { Status } from '../Status';
import {
  ATTACK_DAMAGE_MOD,
  ATTACK_DAMAGE_TAKEN_MOD,
  type IAttackDamageModifier,
  type IAttackDamageTakenModifier,
} from '../../core/Modifiers';

/**
 * Adds +1 to both attack damage dealt and taken.
 * Linked to a Bloodstone enemy; removes itself when the owner dies or is on a different floor.
 * Port of C# BloodstoneStatus.
 */
export class BloodstoneStatus extends Status implements IAttackDamageModifier, IAttackDamageTakenModifier {
  readonly [ATTACK_DAMAGE_MOD] = true as const;
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;

  private owner: any; // Bloodstone enemy ref

  constructor(owner: any) {
    super();
    this.owner = owner;
  }

  Consume(_other: Status): boolean {
    return false;
  }

  /** Check if owner is still alive and on same floor. */
  refresh(): void {
    const ownerDead = this.owner?.isDead;
    const differentFloor = this.actor?.floor !== this.owner?.floor;
    if (ownerDead || differentFloor) {
      this.Remove();
    }
  }

  Step(): void {
    this.refresh();
  }

  // Both IAttackDamageModifier and IAttackDamageTakenModifier use this
  modify(input: any): any {
    if (typeof input === 'number') {
      return input + 1;
    }
    // STEP_MOD path
    this.Step();
    return input;
  }
}
