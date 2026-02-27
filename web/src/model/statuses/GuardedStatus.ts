import { ATTACK_DAMAGE_TAKEN_MOD, type IAttackDamageTakenModifier } from '../../core/Modifiers';
import { StackingStatus } from '../Status';
import { Status } from '../Status';
import { BaseAction } from '../BaseAction';

/**
 * Absorbs attack damage via Guardleaf's guardLeft pool.
 * Stacks delegate to the leaf's guardLeft.
 * Port of C# GuardedStatus from Guardleaf.cs.
 */
export class GuardedStatus extends StackingStatus implements IAttackDamageTakenModifier {
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;

  /** Duck-type access to the Guardleaf grass under the actor. */
  private get leaf(): any {
    const grass = this.actor?.grass;
    return grass && 'guardLeft' in grass ? grass : null;
  }

  get stacks(): number {
    return this.leaf?.guardLeft ?? 0;
  }

  set stacks(_value: number) {
    // stacks delegated to leaf.guardLeft — don't store locally
  }

  constructor() {
    super(0);
  }

  /** Handles STEP_MOD (inherited) and ATTACK_DAMAGE_TAKEN_MOD. */
  modify(input: any): any {
    if (typeof input === 'number') {
      // ATTACK_DAMAGE_TAKEN_MOD: absorb damage
      const leaf = this.leaf;
      if (leaf) {
        const reduction = Math.min(input, leaf.guardLeft);
        leaf.removeGuard(reduction);
        leaf.onNoteworthyAction?.();
        return input - reduction;
      } else {
        this.Remove();
        return input;
      }
    }
    // STEP_MOD
    return super.modify(input);
  }

  Step(): void {
    if (!this.leaf || this.stacks <= 0) {
      this.Remove();
    }
  }

  Consume(_other: Status): boolean {
    return true;
  }
}
