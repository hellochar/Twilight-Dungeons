import { EquippableItem, DURABLE_TAG, reduceDurability, type IDurable } from '../Item';
import { EquipmentSlot } from '../Equipment';
import { BODY_MOVE_HANDLER, type IBodyMoveHandler } from '../Body';
import { GameModelRef } from '../GameModelRef';
import { ConfusedStatus } from '../statuses/ConfusedStatus';
import { Faction } from '../../core/types';
import { MyRandom } from '../../core/MyRandom';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Actor } from '../Actor';

/**
 * Headwear that confuses a random adjacent enemy on move.
 * Port of C# ItemWildwoodWreath from Wildwood.cs.
 */
export class ItemWildwoodWreath
  extends EquippableItem
  implements IDurable, IBodyMoveHandler
{
  readonly [DURABLE_TAG] = true as const;
  readonly [BODY_MOVE_HANDLER] = true as const;

  durability: number;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Headwear;
  }

  get maxDurability(): number {
    return 4;
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  handleMove(newPos: Vector2Int, _oldPos: Vector2Int): void {
    const floor = GameModelRef.main.player.floor;
    if (!floor) return;

    const adjacentEnemies = floor.adjacentBodies(newPos).filter(
      b => 'faction' in b && (b as any).faction === Faction.Enemy
    );

    if (adjacentEnemies.length > 0) {
      const target = MyRandom.Pick(adjacentEnemies) as Actor;
      target.statuses.add(new ConfusedStatus(5));
      reduceDurability(this);
    }
  }

  getStats(): string {
    return 'On move: confuse a random adjacent enemy for 5 turns.';
  }
}
