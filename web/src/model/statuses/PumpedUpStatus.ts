import { ACTION_COST_MOD, type IActionCostModifier } from '../../core/Modifiers';
import { StackingStatus } from '../Status';
import { ACTION_PERFORMED_HANDLER, type IActionPerformedHandler } from '../Actor';
import { ActionType } from '../../core/types';
import { BaseAction, type ActionCosts } from '../BaseAction';

/**
 * Halves attack cost for N attacks, then expires.
 * Port of C# PumpedUpStatus from ItemMushroom.cs.
 */
export class PumpedUpStatus extends StackingStatus implements IActionCostModifier, IActionPerformedHandler {
  readonly [ACTION_COST_MOD] = true as const;
  readonly [ACTION_PERFORMED_HANDLER] = true as const;

  constructor(stacks = 1) {
    super(stacks);
  }

  /** Handles STEP_MOD (inherited) and ACTION_COST_MOD. */
  modify(input: any): any {
    if (input instanceof Map) {
      const costs = input as ActionCosts;
      costs.set(ActionType.ATTACK, (costs.get(ActionType.ATTACK) ?? 1) / 2);
      return costs;
    }
    // STEP_MOD
    return super.modify(input);
  }

  handleActionPerformed(finalAction: BaseAction, _initialAction: BaseAction): void {
    if (finalAction.type === ActionType.ATTACK) {
      this.stacks--;
    }
  }
}
