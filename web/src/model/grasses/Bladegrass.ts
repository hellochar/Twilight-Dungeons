import { Grass } from './Grass';
import { ACTOR_ENTER_HANDLER, ACTOR_LEAVE_HANDLER, type IActorEnterHandler, type IActorLeaveHandler } from '../../core/types';
import { Ground } from '../Tile';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';

/**
 * Sharpens when an actor leaves. When sharp, deals 2 damage on enter then dies.
 * Auto-dies after 10 turns once sharpened.
 * Port of C# Bladegrass.cs.
 */
export class Bladegrass extends Grass implements IActorEnterHandler, IActorLeaveHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;
  readonly [ACTOR_LEAVE_HANDLER] = true as const;

  isSharp = false;
  onSharpened: (() => void) | null = null;

  constructor(pos: Vector2Int) {
    super(pos);
  }

  static canOccupy(tile: Tile): boolean {
    return tile instanceof Ground;
  }

  sharpen(): void {
    if (!this.isSharp) {
      this.isSharp = true;
      this.onSharpened?.();
      // this.addTimedEvent(10, () => this.killSelf());
    }
  }

  handleActorLeave(_actor: any): void {
    this.sharpen();
  }

  handleActorEnter(actor: any): void {
    if (this.isSharp) {
      this.kill(actor);
      actor.takeDamage(1, this);
    }
  }
}

entityRegistry.register('Bladegrass', Bladegrass);
