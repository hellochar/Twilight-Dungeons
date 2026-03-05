import { ATTACK_DAMAGE_TAKEN_MOD, type IAttackDamageTakenModifier } from '../../core/Modifiers';
import { StackingStatus, Status } from '../Status';

/**
 * Absorbs attack damage via Guardleaf's guardLeft pool.
 * Stacks delegate to the leaf's guardLeft.
 * Port of C# GuardedStatus from Guardleaf.cs.
 */
export class GuardedStatus extends Status implements IAttackDamageTakenModifier {
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;

  /** Duck-type access to the Guardleaf grass under the actor. */
  private get leaf(): any {
    const grass = this.actor?.grass;
    return grass && 'guardLeft' in grass ? grass : null;
  }

  constructor() {
    super();
  }

  /** Handles STEP_MOD (inherited) and ATTACK_DAMAGE_TAKEN_MOD. */
  modify(input: any): any {
    if (typeof input === 'number') {
      // ATTACK_DAMAGE_TAKEN_MOD: absorb damage
      const leaf = this.leaf;
      if (leaf) {
        const reduction = input;
        leaf.removeGuard();
        leaf.onNoteworthyAction?.();
        this.Remove();
        return 0;
      } else {
        this.Remove();
        return input;
      }
    }
    // STEP_MOD
    return super.modify(input);
  }

  Step(): void {
    if (!this.leaf) {
      this.Remove();
    }
  }

  Consume(_other: Status): boolean {
    return true;
  }
}
