import { StackingStatus, type Status } from '../Status';
import { GameModelRef } from '../GameModelRef';
import { Vector2Int } from '../../core/Vector2Int';
import { entityRegistry } from '../../generator/entityRegistry';

/**
 * At 10 stacks, you die.
 * Removes on floor cleared.
 * Port of C# ClumpedLungStatus.
 */
export class ClumpedLungStatus extends StackingStatus {
  private floorClearedUnsub: (() => void) | null = null;

  get isDebuff(): boolean {
    return true;
  }

  Start(): void {
    const model = GameModelRef.mainOrNull;
    if (model) {
      this.floorClearedUnsub = model.onFloorCleared.on(() => this.Remove());
    }
  }

  End(): void {
    this.floorClearedUnsub?.();
    this.floorClearedUnsub = null;
  }

  Consume(other: Status): boolean {
    const baseRetVal = super.Consume(other);
    if (this.stacks >= 10) {
      // Create a temporary Clumpshroom as the kill source, matching C#
      const source = entityRegistry.create('Clumpshroom', new Vector2Int(0, 0));
      if (source) this.actor?.kill(source);
    }
    return baseRetVal;
  }
}
