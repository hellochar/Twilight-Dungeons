import { Grass } from './Grass';
import { ACTOR_ENTER_HANDLER, type IActorEnterHandler } from '../../core/types';
import { GameModelRef } from '../GameModelRef';
import { ItemAstoria } from '../items/ItemAstoria';
import { ItemOnGround } from '../ItemOnGround';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';
import { Ground } from '../Tile';

/**
 * Heals you for 4 HP if hurt. If at full HP, becomes an ItemAstoria in inventory.
 * Port of C# Astoria.cs.
 */
export class Astoria extends Grass implements IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;

  constructor(pos: Vector2Int) {
    super(pos);
  }

  static canOccupy(tile: Tile): boolean {
    return tile instanceof Ground;
  }

  handleActorEnter(actor: any): void {
    const player = GameModelRef.main.player;
    if (actor === player) {
      if (actor.hp < actor.maxHp) {
        actor.heal(4);
        this.kill(actor);
      } else {
        this.becomeItemInInventory(new ItemAstoria());
      }
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

entityRegistry.register('Astoria', Astoria);
