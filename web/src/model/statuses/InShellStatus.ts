import { ATTACK_DAMAGE_TAKEN_MOD, BASE_ACTION_MOD, type IAttackDamageTakenModifier, type IBaseActionModifier } from '../../core/Modifiers';
import { StackingMode, StackingStatus } from '../Status';
import { WaitBaseAction, BaseAction } from '../BaseAction';

/**
 * Snail retreats into shell: forces WAIT, reduces damage taken by 1.
 * Default 4 stacks, decrements each turn.
 * Port of C# InShellStatus from Snail.cs.
 */
export class InShellStatus extends StackingStatus implements IAttackDamageTakenModifier, IBaseActionModifier {
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;
  readonly [BASE_ACTION_MOD] = true as const;

  get stackingMode(): StackingMode {
    return StackingMode.Max;
  }

  constructor(stacks = 4) {
    super(stacks);
  }

  /** Handles STEP_MOD (inherited), ATTACK_DAMAGE_TAKEN_MOD, and BASE_ACTION_MOD. */
  modify(input: any): any {
    if (typeof input === 'number') {
      // ATTACK_DAMAGE_TAKEN_MOD: reduce damage by 1
      return input - 1;
    }
    if (input instanceof BaseAction) {
      // BASE_ACTION_MOD: force wait, decrement stacks
      this.stacks--;
      return new WaitBaseAction(input.actor);
    }
    // STEP_MOD call
    return super.modify(input);
  }
}
