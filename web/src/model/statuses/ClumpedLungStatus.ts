import { StackingStatus } from '../Status';
import { Vector2Int } from '../../core/Vector2Int';

/**
 * At 20 stacks, you die.
 * Resets on floor cleared (debuff auto-removes on floor change).
 * Port of C# ClumpedLungStatus.
 */
export class ClumpedLungStatus extends StackingStatus {
  get isDebuff(): boolean {
    return true;
  }

  Consume(other: import('../Status').Status): boolean {
    const baseRetVal = super.Consume(other);
    if (this.stacks >= 20) {
      // Import avoided at top level to prevent circular dep;
      // create a temporary Clumpshroom-like source for the kill
      this.actor?.kill({ pos: new Vector2Int(0, 0) } as any);
    }
    return baseRetVal;
  }
}
