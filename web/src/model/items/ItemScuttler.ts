import { Item, USABLE_TAG, type IUsable } from '../Item';
import { ScuttlerUnderground } from '../enemies/Scuttler';
import type { Actor } from '../Actor';

/**
 * Place a Scuttler Underground on an adjacent valid tile.
 * C# uses ITargetedAction<Tile>; web port auto-places on first valid adjacent tile.
 * Port of C# ItemScuttler.
 */
export class ItemScuttler extends Item implements IUsable {
  readonly [USABLE_TAG] = true as const;

  use(actor: Actor): void {
    const floor = actor.floor;
    if (!floor) return;

    const validTiles = floor.getAdjacentTiles(actor.pos)
      .filter(t => ScuttlerUnderground.canOccupy(t));

    if (validTiles.length > 0) {
      floor.put(new ScuttlerUnderground(validTiles[0].pos));
      this.Destroy();
    }
  }

  getStats(): string {
    return 'Places a Scuttler Underground on an adjacent tile. Anything that walks over it becomes targeted.';
  }
}
