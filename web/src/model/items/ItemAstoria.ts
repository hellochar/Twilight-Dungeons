import { Item, USABLE_TAG, type IUsable } from '../Item';
import type { Actor } from '../Actor';

/**
 * Usable consumable. Heals 4 HP, then destroys itself.
 * Port of C# ItemAstoria from Astoria.cs.
 */
export class ItemAstoria extends Item implements IUsable {
  readonly [USABLE_TAG] = true as const;

  use(actor: Actor): void {
    actor.heal(4);
    this.Destroy();
  }

  getStats(): string {
    return 'Heals 4 HP.';
  }
}
