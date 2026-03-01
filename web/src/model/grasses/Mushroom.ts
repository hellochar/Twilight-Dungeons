import { Grass } from './Grass';
import { ACTOR_ENTER_HANDLER, type IActorEnterHandler } from '../../core/types';
import { Ground, Wall } from '../Tile';
import { GameModelRef } from '../GameModelRef';
import { ItemMushroom } from '../items/ItemMushroom';
import { ItemOnGround } from '../ItemOnGround';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';

/**
 * Walk over it to harvest. Player picks up an ItemMushroom.
 * Port of C# Mushroom.cs.
 */
export class Mushroom extends Grass implements IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;

  constructor(pos: Vector2Int) {
    super(pos);
  }

  static canOccupy(tile: Tile): boolean {
    const floor = tile.floor;
    if (!floor) return false;
    const isHuggingWall = floor.getCardinalNeighbors(tile.pos).some(t => t instanceof Wall);
    return isHuggingWall && tile instanceof Ground && tile.grass == null;
  }

  handleActorEnter(actor: any): void {
    const player = GameModelRef.main.player;
    if (actor === player) {
      this.becomeItemInInventory(new ItemMushroom(1));
    }
  }

  /** Kill self and add item to player inventory (or drop on ground). */
  private becomeItemInInventory(item: import('../Item').Item): void {
    const player = GameModelRef.main.player;
    const floor = this.floor;
    this.kill(player);
    if (!player.inventory.addItem(item, this)) {
      if (floor) {
        floor.put(new ItemOnGround(this.pos, item, this.pos));
      }
    }
  }
}

entityRegistry.register('Mushroom', Mushroom);
