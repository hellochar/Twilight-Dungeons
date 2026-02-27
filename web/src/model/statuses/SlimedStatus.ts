import { ACTION_COST_MOD, BASE_ACTION_MOD, type IActionCostModifier, type IBaseActionModifier } from '../../core/Modifiers';
import { Status } from '../Status';
import { ActionType } from '../../core/types';
import { BaseAction, type ActionCosts } from '../BaseAction';

/**
 * Doubles move cost. Auto-removes on next MOVE action.
 * Does not stack (Consume returns false).
 * Port of C# SlimedStatus from Snail.cs.
 */
export class SlimedStatus extends Status implements IActionCostModifier, IBaseActionModifier {
  readonly [ACTION_COST_MOD] = true as const;
  readonly [BASE_ACTION_MOD] = true as const;

  get isDebuff(): boolean {
    return true;
  }

  /** Handles STEP_MOD (inherited), ACTION_COST_MOD, and BASE_ACTION_MOD. */
  modify(input: any): any {
    if (input instanceof BaseAction) {
      // BASE_ACTION_MOD: remove on move
      if (input.type === ActionType.MOVE) {
        this.Remove();
      }
      return input;
    }
    if (input instanceof Map) {
      // ACTION_COST_MOD: double move cost
      const costs = input as ActionCosts;
      costs.set(ActionType.MOVE, (costs.get(ActionType.MOVE) ?? 1) * 2);
      return costs;
    }
    // STEP_MOD
    return super.modify(input);
  }

  Consume(_other: Status): boolean {
    return false;
  }
}
