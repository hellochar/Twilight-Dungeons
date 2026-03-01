import { StackingStatus, StackingMode } from '../Status';

/**
 * After N turns, heals the actor for stacks HP.
 * Independent stacking (multiple instances can coexist).
 * Port of C# RecoveringStatus.
 */
export class RecoveringStatus extends StackingStatus {
  get stackingMode(): StackingMode {
    return StackingMode.Independent;
  }

  private turnsLeft = 25;

  constructor(stacks: number) {
    super(stacks);
  }

  Step(): void {
    this.turnsLeft--;
    if (this.turnsLeft <= 0) {
      this.Remove();
    }
  }

  End(): void {
    this.actor?.heal(this.stacks);
  }
}
