import { Item, TARGETED_ACTION_TAG, type ITargetedAction } from '../Item';
import { ScuttlerUnderground } from '../enemies/Scuttler';
import type { Entity } from '../Entity';
import type { Player } from '../Player';

/**
 * Place a Scuttler Underground on an adjacent valid tile.
 * Player selects target tile via ITargetedAction UI.
 * Port of C# ItemScuttler.
 */
export class ItemScuttler extends Item implements ITargetedAction {
  readonly [TARGETED_ACTION_TAG] = true as const;
  readonly targetedActionName = 'Place';

  targets(player: Player): Entity[] {
    const floor = player.floor;
    if (!floor) return [];
    return floor.getAdjacentTiles(player.pos)
      .filter(t => ScuttlerUnderground.canOccupy(t));
  }

  performTargetedAction(player: Player, target: Entity): void {
    player.floor!.put(new ScuttlerUnderground(target.pos));
    this.Destroy();
  }

  getStats(): string {
    return 'Places a Scuttler Underground on an adjacent tile. Anything that walks over it becomes targeted.';
  }
}
