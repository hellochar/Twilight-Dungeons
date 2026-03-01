import { ACTION_COST_MOD, type IActionCostModifier } from '../../core/Modifiers';
import { StackingStatus } from '../Status';
import { ActionType } from '../../core/types';
import type { ActionCosts } from '../BaseAction';

/**
 * Halves move cost. Loses 1 stack per turn.
 * Port of C# StatusWild from ItemWildwoodLeaf.cs.
 */
export class StatusWild extends StackingStatus implements IActionCostModifier {
  readonly [ACTION_COST_MOD] = true as const;

  constructor(stacks = 15) {
    super(stacks);
  }

  /** Handles STEP_MOD (inherited) and ACTION_COST_MOD. */
  modify(input: any): any {
    if (input instanceof Map) {
      const costs = input as ActionCosts;
      costs.set(ActionType.MOVE, (costs.get(ActionType.MOVE) ?? 1) / 2);
      return costs;
    }
    // STEP_MOD
    return super.modify(input);
  }

  Step(): void {
    this.stacks--;
  }
}
