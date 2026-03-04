import { StackingMode, StackingStatus } from '../Status';

/**
 * Poison stacks that tick independently.
 * At 3+ stacks: deal 3 damage, remove 3 stacks.
 * Otherwise: lose 1 stack every 5 turns.
 * Port of C# PoisonedStatus from Spider.cs.
 */
export class PoisonedStatus extends StackingStatus {
  get stackingMode(): StackingMode {
    return StackingMode.Add;
  }

  get isDebuff(): boolean {
    return true;
  }

  private duration = 5;

  constructor(stacks = 1) {
    super(stacks);
  }

  Start(): void {
    this.actor!.addTimedEvent(1, () => this.independentStep());
  }

  private independentStep(): void {
    if (!this.actor || this.stacks <= 0) return;

    if (this.stacks >= 3) {
      // Trigger damage shortly after turn
      this.actor.addTimedEvent(0.01, () => this.tickDamage());
    } else {
      if (--this.duration <= 0) {
        this.stacks = this.stacks - 1;
        this.duration = 5;
      }
      if (this.stacks > 0) {
        this.actor.addTimedEvent(1, () => this.independentStep());
      }
    }
  }

  private tickDamage(): void {
    if (!this.actor || this.stacks <= 0) return;
    this.actor.takeDamage(3, this.actor);
    this.stacks = this.stacks - 3;
    if (this.stacks > 0) {
      // Re-sync timing
      this.actor.addTimedEvent(0.99, () => this.independentStep());
    }
  }
}
