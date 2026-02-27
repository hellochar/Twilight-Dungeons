import { ACTION_COST_MOD, type IActionCostModifier } from '../../core/Modifiers';
import { StackingStatus } from '../Status';
import { ACTION_PERFORMED_HANDLER, type IActionPerformedHandler } from '../Actor';
import { ActionType } from '../../core/types';
import { BaseAction, type ActionCosts } from '../BaseAction';

/**
 * Free move: next movement costs 0 turns. Consumed on MOVE action.
 * Port of C# FreeMoveStatus from SoftGrass.cs.
 */
export class FreeMoveStatus extends StackingStatus implements IActionCostModifier, IActionPerformedHandler {
  readonly [ACTION_COST_MOD] = true as const;
  readonly [ACTION_PERFORMED_HANDLER] = true as const;

  constructor(stacks = 1) {
    super(stacks);
  }

  /** Handles STEP_MOD (inherited), ACTION_COST_MOD. */
  modify(input: any): any {
    if (input instanceof Map) {
      // ACTION_COST_MOD: free movement
      const costs = input as ActionCosts;
      costs.set(ActionType.MOVE, 0);
      return costs;
    }
    // STEP_MOD
    return super.modify(input);
  }

  handleActionPerformed(finalAction: BaseAction, initialAction: BaseAction): void {
    if (initialAction.type === ActionType.MOVE) {
      this.stacks--;
    }
  }
}
