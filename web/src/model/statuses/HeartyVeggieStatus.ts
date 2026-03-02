import { StackingStatus } from '../Status';
import { ACTION_PERFORMED_HANDLER, type IActionPerformedHandler } from '../Actor';
import type { BaseAction } from '../BaseAction';

/**
 * Heals 1 HP every 25 turns per stack. Pauses at full HP.
 * Port of C# HeartyVeggieStatus from StoutShrub.cs.
 */
export class HeartyVeggieStatus extends StackingStatus implements IActionPerformedHandler {
  readonly [ACTION_PERFORMED_HANDLER] = true as const;

  private turnsLeft = 25;

  constructor(stacks: number) {
    super(stacks);
  }

  handleActionPerformed(_finalAction: BaseAction, _initialAction: BaseAction): void {
    const a = this.actor;
    if (!a) return;
    if (a.hp < a.maxHp) {
      this.turnsLeft--;
      if (this.turnsLeft === 0) {
        this.turnsLeft = 25;
        a.heal(1);
        this.stacks--;
      }
    }
  }
}
