import { ACTION_COST_MOD, type IActionCostModifier } from '../../core/Modifiers';
import { StackingMode, StackingStatus } from '../Status';
import { ACTION_PERFORMED_HANDLER, DEAL_ATTACK_DAMAGE_HANDLER, type IActionPerformedHandler, type IDealAttackDamageHandler } from '../Actor';
import { TAKE_ANY_DAMAGE_HANDLER, type ITakeAnyDamageHandler } from '../Body';
import { ActionType } from '../../core/types';
import { BaseAction, type ActionCosts } from '../BaseAction';
import type { Body } from '../Body';

/**
 * Free movement on non-cleared floors. Removed on taking or dealing damage.
 * Port of C# ZenStatus.
 */
export class ZenStatus extends StackingStatus implements IActionCostModifier, IActionPerformedHandler, ITakeAnyDamageHandler, IDealAttackDamageHandler {
  readonly [ACTION_COST_MOD] = true as const;
  readonly [ACTION_PERFORMED_HANDLER] = true as const;
  readonly [TAKE_ANY_DAMAGE_HANDLER] = true as const;
  readonly [DEAL_ATTACK_DAMAGE_HANDLER] = true as const;

  get stackingMode(): StackingMode {
    return StackingMode.Max;
  }

  constructor(stacks: number) {
    super(stacks);
  }

  modify(input: any): any {
    if (input instanceof Map) {
      const costs = input as ActionCosts;
      if (this.actor?.floor && this.actor.floor.enemiesLeft() > 0) {
        costs.set(ActionType.MOVE, 0);
      }
      return costs;
    }
    return super.modify(input);
  }

  handleActionPerformed(finalAction: BaseAction, _initialAction: BaseAction): void {
    if (finalAction.type === ActionType.MOVE && this.actor?.floor && this.actor.floor.enemiesLeft() > 0) {
      this.stacks--;
    }
  }

  handleTakeAnyDamage(damage: number): void {
    if (damage > 0) {
      this.Remove();
    }
  }

  handleDealAttackDamage(_damage: number, _target: Body): void {
    this.Remove();
  }
}
